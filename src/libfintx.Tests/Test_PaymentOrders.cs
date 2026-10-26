using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using libfintx.FinTS;
using libfintx.FinTS.BankParameterData;
using libfintx.FinTS.Data;
using libfintx.FinTS.Data.Segment;
using libfintx.Sepa;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// SEPA credit transfers: pain.001.001.09 (DFÜ-Abkommen Anlage 3 V3.9, 2.2.1/2.2.4), HISPAS parameters and the order
/// segments HKCCS, HKCSE, HKCCM, HKCME (FinTS Messages Geschäftsvorfälle 2022-04-15, C.10.2). Values made up.
/// </summary>
public class Test_PaymentOrders
{
    private const string OwnIban = "DE89100700000099164406";
    private const string Hispa = "HISPA:4:1:3+J:" + OwnIban + ":DEUTDEFFXXX:0099164406:01:280:10070000";
    private const string Accepted = "HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:3+0020::Auftrag ausgeführt.'";

    private static XmlDocument Xml(string pain)
    {
        var document = new XmlDocument();
        document.LoadXml(pain.TrimEnd('\''));
        return document;
    }

    private static XmlNamespaceManager Ns(XmlDocument document)
    {
        var ns = new XmlNamespaceManager(document.NameTable);
        ns.AddNamespace("p", pain00100109.Descriptor);
        return ns;
    }

    [Fact]
    public void Test_Pain00100109_Single()
    {
        var pain = pain00100109.Create("Erika Müller", OwnIban, "DEUTDEFFXXX", "Max Muster's Laden", "DE02100700000123456789",
            null, 1.5m, new string('x', 150), new DateTime(1999, 1, 1), messageId: "MSG1", maxUsageLength: 120);

        var xml = Xml(pain);
        var ns = Ns(xml);
        Assert.Equal("MSG1-1", xml.SelectSingleNode("//p:EndToEndId", ns).InnerText);
        Assert.Equal("1.50", xml.SelectSingleNode("//p:InstdAmt", ns).InnerText);
        Assert.Equal("1999-01-01", xml.SelectSingleNode("//p:ReqdExctnDt/p:Dt", ns).InnerText);
        Assert.Equal("DEUTDEFFXXX", xml.SelectSingleNode("//p:DbtrAgt/p:FinInstnId/p:BICFI", ns).InnerText);
        Assert.Null(xml.SelectSingleNode("//p:CdtrAgt", ns));                       // IBAN only
        Assert.Null(xml.SelectSingleNode("//p:LclInstrm", ns));                     // not instant
        Assert.Equal("Erika Mueller", xml.SelectSingleNode("//p:Dbtr/p:Nm", ns).InnerText);
        Assert.Equal("Max Muster's Laden", xml.SelectSingleNode("//p:Cdtr/p:Nm", ns).InnerText);
        Assert.Equal(120, xml.SelectSingleNode("//p:Ustrd", ns).InnerText.Length); // 140 less reserved positions
    }

    [Fact]
    public void Test_Pain00100109_InstantCollective()
    {
        var items = new List<Pain00100203CtData>
        {
            new Pain00100203CtData { Receiver = "A", ReceiverIban = "DE02100700000123456789", ReceiverBic = "DEUTDEBBXXX", Amount = 1m, Usage = "1" },
            new Pain00100203CtData { Receiver = "B", ReceiverIban = "DE02500105170137075030", Amount = 2.25m, Usage = "2" },
        };

        var pain = pain00100109.Create("Erika Mustermann", OwnIban, null, items, new DateTime(1999, 1, 1), InstantPayment: true, messageId: "M");

        var xml = Xml(pain);
        var ns = Ns(xml);
        Assert.Equal("INST", xml.SelectSingleNode("//p:PmtInf/p:PmtTpInf/p:LclInstrm/p:Cd", ns).InnerText);
        Assert.Equal("2", xml.SelectSingleNode("//p:GrpHdr/p:NbOfTxs", ns).InnerText);
        Assert.Equal("3.25", xml.SelectSingleNode("//p:GrpHdr/p:CtrlSum", ns).InnerText);
        Assert.Equal("NOTPROVIDED", xml.SelectSingleNode("//p:DbtrAgt/p:FinInstnId/p:Othr/p:Id", ns).InnerText);
        Assert.Equal(new[] { "M-1", "M-2" }, xml.SelectNodes("//p:EndToEndId", ns).Cast<XmlNode>().Select(n => n.InnerText));
    }

    [Fact]
    public void Test_Hispas()
    {
        var segment = new GenericSegmentParser().ParseSegment(new Segment(
            "HISPAS:147:3:4+1+1+0+J:N:N:J:20:urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.001.09:urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.001.03'"));
        var hispas = (HISPAS) new HISPASSegmentParser().ParseSegment(segment);

        Assert.False(hispas.IsAccountNationalAllowed);
        Assert.Equal(20, hispas.ReservedPurposeLength);
        Assert.Equal(new[] { "urn:iso:std:iso:20022:tech:xsd:pain.001.001.09", "urn:iso:std:iso:20022:tech:xsd:pain.001.001.03" }, hispas.PainFormats);
    }

    [Theory]
    [InlineData("100", "100,")]
    [InlineData("0.01", "0,01")]
    [InlineData("12.50", "12,5")]
    [InlineData("1234.56", "1234,56")]
    public void Test_ToFinTsAmount(string amount, string expected)
    {
        Assert.Equal(expected, Converter.ToFinTsAmount(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    private static FinTsClient CreateClient(FakeFinTsServer server, int hispas)
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
        client.BPD = BPD.Parse("HIBPA:4:3:5+1+280:10070000+Fake Bank+0+1+300+9999'");
        client.HISPAS = hispas;
        client.HISPAS_Pain = 4;
        return client;
    }

    private static FakeFinTsServer Bank(string orderId) => new FakeFinTsServer(request =>
        request.Contains("HKSPA:") ? "HNHBK:1:3+000000000100+300+DIALOG1+2'HIRMG:2:2+0010::Nachricht entgegengenommen.'" + Hispa + "'HNHBS:5:1+2'"
        : request.Contains(orderId + ":") ? "HNHBK:1:3+000000000100+300+DIALOG1+3'" + Accepted + "HNHBS:4:1+3'"
        : FakeFinTsServer.DefaultResponse);

    /// <summary>"@length@data" of the order segment: the length must match the data.</summary>
    private static string Pain(string segment)
    {
        var match = Regex.Match(segment, @"\+@(\d+)@(.*)'$", RegexOptions.Singleline);
        Assert.True(match.Success);
        Assert.Equal(int.Parse(match.Groups[1].Value), match.Groups[2].Value.Length);
        return match.Groups[2].Value;
    }

    [Fact]
    public async Task Test_Transfer_AccountFromHispa()
    {
        using var server = Bank("HKCCS");

        var result = await CreateClient(server, 3).Transfer(null, "Max Muster", "DE02100700000123456789", "DEUTDEBBXXX", 1.5m, "Rechnung 4711", null);

        Assert.Equal(OrderStatus.Accepted, result.GetOrderStatus());
        var segment = server.Segment("HKCCS");
        // KTI as reported in HISPA, not only IBAN and BIC (some banks answer 9210 "Keine gültige Kontoverbindung")
        Assert.StartsWith("HKCCS:3:1+" + OwnIban + ":DEUTDEFFXXX:0099164406:01:280:10070000+urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.001.09+@", segment);
        Assert.Contains("<Cdtr><Nm>Max Muster</Nm></Cdtr>", Pain(segment));
    }

    [Fact]
    public async Task Test_Transfer_WithoutHkspa()
    {
        using var server = Bank("HKCCS");

        await CreateClient(server, 0).Transfer(null, "Max Muster", "DE02100700000123456789", null, 1m, "x", null);

        Assert.DoesNotContain(server.Requests, r => r.Contains("HKSPA:"));
        Assert.StartsWith("HKCCS:3:1+" + OwnIban + ":DEUTDEFFXXX+urn", server.Segment("HKCCS"));
    }

    [Fact]
    public async Task Test_TerminatedTransfer()
    {
        using var server = Bank("HKCSE");

        await CreateClient(server, 3).Transfer_Terminated(null, "Max Muster", "DE02100700000123456789", null, 1m, "x", new DateTime(2026, 12, 1), null);

        Assert.Contains("<ReqdExctnDt><Dt>2026-12-01</Dt></ReqdExctnDt>", Pain(server.Segment("HKCSE")));
    }

    [Theory]
    [InlineData(false, "HKCCM")]
    [InlineData(true, "HKCME")]
    public async Task Test_CollectiveTransfer(bool terminated, string segmentId)
    {
        using var server = Bank(segmentId);
        var client = CreateClient(server, 3);
        var items = new List<Pain00100203CtData>
        {
            new Pain00100203CtData { Receiver = "A", ReceiverIban = "DE02100700000123456789", Amount = 1m, Usage = "1" },
            new Pain00100203CtData { Receiver = "B", ReceiverIban = "DE02500105170137075030", Amount = 2.5m, Usage = "2" },
        };

        var result = terminated
            ? await client.CollectiveTransfer_Terminated(null, items, "2", 3.5m, new DateTime(2026, 12, 1), null)
            : await client.CollectiveTransfer(null, items, "2", 3.5m, null);

        Assert.Equal(OrderStatus.Accepted, result.GetOrderStatus());
        var segment = server.Segment(segmentId);
        // account, total (Summenfeld, btg "wrt:EUR"), single booking requested (empty), descriptor, pain
        Assert.StartsWith(segmentId + ":3:1+" + OwnIban + ":DEUTDEFFXXX:0099164406:01:280:10070000+3,5:EUR++urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.001.09+@", segment);
        Assert.Contains("<NbOfTxs>2</NbOfTxs>", Pain(segment));
    }
}
