using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using libfintx.FinTS;
using libfintx.FinTS.BankParameterData;
using libfintx.FinTS.Data;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// SEPA instant payments ("FinTS 3.0 Messages Geschäftsvorfälle" 2022-04-15, C.10.2.9 and C.10.3.5) and pain.001.001.09
/// with local instrument INST (DFÜ-Abkommen Anlage 3 V3.9, 2.2.4). Bank answers rebuilt, values made up.
/// </summary>
public class Test_InstantPayment
{
    private const string OwnIban = "DE89100700000099164406";
    private const string Payee = "DE02100700000123456789";
    private const string Pain09 = "urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.001.09";
    private const string Psr = "urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.002.001.10";

    private const string Bpd = "HIBPA:5:3:3+12+280:10070000+Testbank+0+1+300+500'"
        + "HIPINS:78:1:4+1+1+0+5:20:6:Benutzerkennung:Kunden-ID:HKIPZ:J:HKIPM:J:HKIPS:N:HKISS:N:HKVPP:N:HKVPA:N'"
        + "HITANS:6:6:4+1+1+0+N:N:0:902:2:TAN1:::TAN-Verfahren:7:1:Freigabe:999:N:1:N:0:2:N:J:00:0:N:8'"
        + "HIIPZS:58:1:4+990+1+0+:" + Pain09 + "'"
        + "HIIPSS:59:1:4+990+1+0+99'"
        + "HIIPMS:64:1:4+1+1+0+500:J::" + Pain09 + "'"
        + "HIISSS:66:1:4+1+1+0+10:" + Psr + ":" + Pain09 + "'";

    private static string Message(string inner) => "HNHBK:1:3+000000000100+300+DIALOG1+2'" + inner + "HNHBS:9:1+2'";

    private static FinTsClient CreateClient(FakeFinTsServer server, string bpd = Bpd)
    {
        var client = TestHelper.CreateTestClient(new ConnectionDetails
        {
            Url = server.Url,
            Blz = 10070000,
            UserId = "user",
            Pin = "pin",
            CustomerSystemId = "SYS",
            Iban = OwnIban,
            Bic = "DEUTDEFFXXX",
            AccountHolder = "Erika Mustermann",
        });
        client.BPD = BPD.Parse(bpd);
        client.HITANS = 6;
        client.HIRMS = "902";
        return client;
    }

    [Fact]
    public void Test_Parameters()
    {
        using var server = new FakeFinTsServer();
        var client = CreateClient(server);

        Assert.True(client.InstantPaymentAvailable());
        Assert.True(client.InstantPaymentAvailable(collective: true));
        Assert.Equal(TimeSpan.FromSeconds(99), client.InstantPaymentStatusWait());
        // HIISSS: minimum wait for collective orders in MINUTES (data dictionary "Mindestwartezeit Sammelzahlung")
        Assert.Equal(TimeSpan.FromMinutes(10), client.InstantPaymentStatusWait(collective: true));

        var withoutStatus = CreateClient(server, "HIBPA:5:3:3+12+280:10070000+Testbank+0+1+300+500'HIIPZS:58:1:4+990+1+0+:" + Pain09 + "'");
        Assert.Null(withoutStatus.InstantPaymentStatusWait());
        Assert.False(CreateClient(server, "HIBPA:5:3:3+12+280:10070000+Testbank+0+1+300+500'").InstantPaymentAvailable());
    }

    [Theory]
    [InlineData("7", "0020", false, InstantPaymentStatus.Executed)]
    [InlineData("3", "3045", false, InstantPaymentStatus.Pending)]
    [InlineData(null, "3045", false, InstantPaymentStatus.Pending)]
    [InlineData("5", "", false, InstantPaymentStatus.Rejected)]          // R transaction
    [InlineData("6", "0020", true, InstantPaymentStatus.Rejected)]
    [InlineData(null, "3042", false, InstantPaymentStatus.Rejected)]
    [InlineData(null, "3270", false, InstantPaymentStatus.ConvertedToCreditTransfer)]
    // the return values of a status request describe the request, only the status field the payment
    [InlineData(null, "9210", true, InstantPaymentStatus.Unknown)]
    [InlineData(null, "0020", true, InstantPaymentStatus.Unknown)]
    public void Test_Status(string statusCode, string codes, bool statusRequest, InstantPaymentStatus expected)
    {
        var list = codes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        Assert.Equal(expected, InstantPayment.Status(statusCode, list, statusRequest));
    }

    private static FakeFinTsServer Bank(string order, string tan = null, string status = null) => new FakeFinTsServer(request =>
        request.Contains("HKIPZ:") || request.Contains("HKIPM:") ? Message(order)
        : request.Contains("HKIPS:") || request.Contains("HKISS:") ? Message(status)
        : request.Contains("HKTAN:") && tan != null ? Message(tan)
        : FakeFinTsServer.DefaultResponse);

    [Fact]
    public async Task Test_InstantTransfer()
    {
        using var server = Bank("HIRMS:3:2:3+3045::SEPA-Instant Payment Statusabfrage HKIPS veranlassen.'HIIPZ:4:1:3+ORDER-1++3'");

        var result = await CreateClient(server).InstantTransfer(null, "Max Muster", Payee, null, 0.01m, "Test", null);

        var segment = server.Segment("HKIPZ");
        Assert.StartsWith("HKIPZ:3:1+" + OwnIban + ":DEUTDEFFXXX+" + Pain09 + "+@", segment);
        Assert.Contains("<LclInstrm><Cd>INST</Cd></LclInstrm>", segment);
        Assert.Equal(OrderStatus.Accepted, result.GetOrderStatus(instantPayment: true));
        Assert.Equal("ORDER-1", result.Data.OrderId);
        Assert.Equal(InstantPaymentStatus.Pending, result.Data.Status);
        Assert.Equal("3", result.Data.StatusCode);
    }

    [Fact]
    public async Task Test_InstantTransfer_ConversionOnlyWhenOffered()
    {
        using var server = Bank("HIRMS:3:2:3+3270::Als SEPA-Überweisung weitergeleitet'HIIPZ:4:2:3+ORDER-2'");
        var bpd = Bpd.Replace("HIIPZS:58:1:4+990+1+0+:", "HIIPZS:58:2:4+990+1+0+J::");

        var result = await CreateClient(server, bpd).InstantTransfer(null, "Max Muster", Payee, null, 1m, "Test", null, allowConversion: true);

        Assert.StartsWith("HKIPZ:3:2+", server.Segment("HKIPZ"));
        Assert.EndsWith("</Document>+J'", server.Segment("HKIPZ"));
        Assert.Equal(InstantPaymentStatus.ConvertedToCreditTransfer, result.Data.Status);
    }

    [Fact]
    public async Task Test_InstantTransfer_WithVerificationOfPayee()
    {
        var bpd = Bpd + "HIVPPS:40:1:4+1+1+0+0:J:V:N:N:" + Psr + ":HKIPZ:HKIPM'";
        using var server = Bank(
            "HIRMS:3:2:3+0020::Namensabgleich ausgeführt'HIRMS:4:2:5+0030::Sicherheitsfreigabe erforderlich'HITAN:5:6:5+4++REF-1+Bitte TAN eingeben'"
            + "HIVPP:6:1:3+@4@V-01+++++" + Payee + "::::RCVC'",
            tan: "HIRMS:3:2:3+0020::Namensabgleich ausgeführt'HIRMS:4:2:4+3045::Statusabfrage HKIPS veranlassen.'HIIPZ:5:1:4+ORDER-3++3'");
        var dialog = new TANDialog(_ => Task.FromResult("123456")) { ConfirmVerificationOfPayeeAsync = _ => Task.FromResult(true) };

        var result = await CreateClient(server, bpd).InstantTransfer(dialog, "Max Muster", Payee, null, 1m, "Test", null);

        Assert.Contains(server.Requests, r => r.Contains("HKVPP:3:1") && r.Contains("HKIPZ:4:1"));
        Assert.Contains("HKVPA:3:1+@4@V-01'", server.Requests.Last());
        Assert.Equal(OrderStatus.Accepted, result.GetOrderStatus(instantPayment: true));
        Assert.Equal("ORDER-3", result.Data.OrderId);
        Assert.Equal(InstantPaymentStatus.Pending, result.Data.Status);
    }

    [Fact]
    public async Task Test_StatusRequest()
    {
        using var server = Bank("", status: "HIRMS:3:2:3+0020::Auftrag ausgeführt.'HIIPS:4:1:3+" + OwnIban + ":DEUTDEFFXXX+" + Pain09 + "++ORDER-1++7'");

        var result = await CreateClient(server).GetInstantPaymentStatus(null, "ORDER-1");

        Assert.Equal("HKIPS:3:1+" + OwnIban + ":DEUTDEFFXXX+" + Pain09 + "+ORDER-1'", server.Segment("HKIPS"));
        Assert.Equal(InstantPaymentStatus.Executed, result.Data.Status);
        Assert.Equal("7", result.Data.StatusCode);
    }

    [Fact]
    public async Task Test_CollectiveStatusRequest()
    {
        using var server = Bank("", status: "HIRMS:3:2:3+0020::Auftrag ausgeführt.'");

        var result = await CreateClient(server).GetInstantPaymentStatus(null, "ORDER-4", collective: true);

        Assert.Equal("HKISS:3:1+" + OwnIban + ":DEUTDEFFXXX+" + Psr + "+" + Pain09 + "+ORDER-4'", server.Segment("HKISS"));
        Assert.Equal(InstantPaymentStatus.Unknown, result.Data.Status);
    }

    [Fact]
    public async Task Test_NothingSentWhenNotOffered()
    {
        using var server = Bank("");
        var client = CreateClient(server, "HIBPA:5:3:3+12+280:10070000+Testbank+0+1+300+500'HIIPZS:58:1:4+990+1+0+:" + Pain09 + "'");

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetInstantPaymentStatus(null, "ORDER-1"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CollectiveInstantTransfer(null,
            new List<libfintx.Sepa.Pain00100203CtData> { new libfintx.Sepa.Pain00100203CtData(), new libfintx.Sepa.Pain00100203CtData() }, "2", 2m, null));
        Assert.Empty(server.Requests);
    }
}
