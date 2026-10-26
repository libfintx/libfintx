using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using libfintx.FinTS;
using libfintx.FinTS.BankParameterData;
using libfintx.FinTS.Data;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// TAN step of an order: additional segments with the TAN (VOP 1.02, figure 3), decoupled approval without automated
/// status requests (HITANS#7 element 26), challenge text, photoTAN image, 3072 and 3920. Values made up.
/// </summary>
public class Test_TanProcess
{
    private const string OrderReference = "REF-4711";

    private static FinTsClient CreateClient(FakeFinTsServer server, int hktanVersion, string procedure, string bpd = null)
    {
        var client = TestHelper.CreateTestClient(new ConnectionDetails
        {
            Url = server.Url,
            Blz = 12345678,
            UserId = "testuser",
            Pin = "testpin",
            CustomerSystemId = "0",
        });
        client.BPD = BPD.Parse(bpd ?? "HIBPA:5:3:3+12+280:12345678+Testbank+0+1+300+500'");
        client.HITANS = hktanVersion;
        client.HIRMS = procedure;
        client.HITAN = OrderReference;
        return client;
    }

    /// <summary>Data elements of the segments inside the encrypted part of a message, by segment id.</summary>
    private static Dictionary<string, List<string>> Segments(string message)
    {
        var match = Regex.Match(message, @"HNVSD:\d+:\d+\+@(\d+)@");
        var inner = message.Substring(match.Index + match.Length, int.Parse(match.Groups[1].Value));
        return Helper.SplitSegments(inner)
            .Select(s => s.EndsWith("'") && !s.EndsWith("?'") ? s.Substring(0, s.Length - 1) : s)
            .ToDictionary(s => s.Substring(0, 5), s => Helper.SplitDataElements(s));
    }

    private static (string, int) Hkvpa(int number) => ("HKVPA:" + number + ":1+@2@V1'", number);

    private static HBCIDialogResult TanRequired() => new HBCIDialogResult(
        new[] { new HBCIBankMessage("0030", "Auftrag empfangen - Sicherheitsfreigabe erforderlich") },
        "HIRMS:3:2:4+0030::Auftrag empfangen - Sicherheitsfreigabe erforderlich'HITAN:4:6:4+4++" + OrderReference + "+Bitte TAN eingeben'");

    private static HBCIDialogResult ApprovalRequired() => new HBCIDialogResult(
        new[] { new HBCIBankMessage("3955", "Sicherheitsfreigabe erfolgt über anderen Kanal") },
        "HIRMS:3:2:4+3955::Sicherheitsfreigabe erfolgt über anderen Kanal'HITAN:4:7:4+4++" + OrderReference + "+Bitte in der App freigeben'");

    private static Task<HBCIDialogResult> ProcessSca(FinTsClient client, HBCIDialogResult result, TANDialog dialog,
        Func<int, (string, int)> extraSegments) =>
        (Task<HBCIDialogResult>) typeof(FinTsClient).GetMethod("ProcessSCA", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(client, new object[] { result, dialog, false, extraSegments });

    [Theory]
    [InlineData(6, "1234567", "2")]
    [InlineData(7, "1234567", "2")]
    [InlineData(7, null, "S")]
    public async Task Test_TanWithAdditionalSegment(int hktanVersion, string tan, string process)
    {
        using var server = new FakeFinTsServer();
        var client = CreateClient(server, hktanVersion, "902");

        var result = await client.TAN(tan, Hkvpa);

        var segments = Segments(server.Requests.Single());
        Assert.Equal("HKVPA:3:1", segments["HKVPA"][0]);
        var hktan = segments["HKTAN"];
        Assert.Equal($"HKTAN:4:{hktanVersion}", hktan[0]);
        Assert.Equal(process, hktan[1]);                  // 2: TAN process
        Assert.Equal(OrderReference, hktan[5]);           // 6: order reference
        Assert.Equal("N", hktan[6]);                      // 7: Weitere TAN folgt
        Assert.Equal(7, hktan.Count);
        Assert.Equal(tan == null ? "testpin" : "testpin:" + tan, segments["HNSHA"][3]);
        Assert.Contains(3, result.IgnoredSegmentReferences); // HKVPA answers do not count for the order
    }

    [Fact]
    public async Task Test_TanWithAdditionalSegment_NeedsHktan6()
    {
        using var server = new FakeFinTsServer();
        await Assert.ThrowsAsync<NotSupportedException>(() => CreateClient(server, 5, "902").TAN("1", Hkvpa));
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task Test_CancelledTanSendsNothing()
    {
        using var server = new FakeFinTsServer();
        var dialog = new TANDialog(_ => Task.FromResult<string>(null));

        await ProcessSca(CreateClient(server, 6, "902"), TanRequired(), dialog, Hkvpa);

        Assert.Empty(server.Requests);
        Assert.Equal("Bitte TAN eingeben", dialog.Challenge);
    }

    private const string ManualConfirmationBpd = "HIBPA:5:3:3+12+280:12345678+Testbank+0+1+300+500'"
        + "HITANS:13:7:4+1+1+0+N:N:0:903:2:Push:Decoupled::Push:::Push:999:N:1:N:0:2:N:J:00:0:N:8:60:0:0:N:N'";

    [Fact]
    public async Task Test_ManualConfirmation_FirstStatusRequestCarriesSegment()
    {
        using var server = new FakeFinTsServer();
        var dialog = new TANDialog(_ => Task.FromResult("confirmed"));

        await ProcessSca(CreateClient(server, 7, "903", ManualConfirmationBpd), ApprovalRequired(), dialog, Hkvpa);

        var segments = Segments(server.Requests.Single());
        Assert.Equal("HKVPA:3:1", segments["HKVPA"][0]);
        Assert.Equal("S", segments["HKTAN"][1]);
    }

    [Fact]
    public async Task Test_ManualConfirmation_CancelSendsNothing()
    {
        using var server = new FakeFinTsServer();
        var dialog = new TANDialog(_ => Task.FromResult<string>(null));

        await ProcessSca(CreateClient(server, 7, "903", ManualConfirmationBpd), ApprovalRequired(), dialog, Hkvpa);
        // with automated status requests the first one would follow after 1 second
        await Task.Delay(1500);

        Assert.Empty(server.Requests);
    }

    [Fact]
    public void Test_Challenge()
    {
        var withText = new HBCIDialogResult(new List<HBCIBankMessage>(),
            "HIRMS:3:2:4+0030::Auftrag empfangen'HITAN:4:6:4+4++REF+Bitte die TAN?: eingeben.<br>Danke'");
        Assert.Equal("Bitte die TAN: eingeben. Danke", Helper.ParseChallenge(withText));

        var withoutText = new HBCIDialogResult(new[] { new HBCIBankMessage("0030", "Sicherheitsfreigabe erforderlich") },
            "HIRMS:3:2:4+0030::Sicherheitsfreigabe erforderlich'HITAN:4:6:4+4++REF'");
        Assert.Equal("Sicherheitsfreigabe erforderlich", Helper.ParseChallenge(withoutText));
    }

    [Fact]
    public async Task Test_PhotoTanImageWithoutProcessName()
    {
        // binary challenge: 2 bytes length of the MIME type, MIME type, 2 bytes length, image
        const string image = "\u0000\u0009image/png\u0000\u0004PNG!";
        using var server = new FakeFinTsServer();
        var client = CreateClient(server, 6, "902",
            "HIBPA:5:3:3+12+280:12345678+Testbank+0+1+300+500'HITANS:6:6:4+1+1+0+N:N:0:902:2:MS1.0.0:::Banking-App:7:1:Freigabe:999:N:1:N:0:2:N:J:00:0:N:8'");
        var result = new HBCIDialogResult(new[] { new HBCIBankMessage("0030", "Sicherheitsfreigabe erforderlich") },
            "HIRMS:3:2:4+0030::Sicherheitsfreigabe erforderlich'HITAN:4:6:4+4++REF+Bitte scannen+@" + image.Length + "@" + image + "'HNSHA:5:2+1'");
        var dialog = new TANDialog(_ => Task.FromResult("1234567"));

        await Helper.WaitForTanAsync(client, result, dialog);

        Assert.Equal("image/png", dialog.MatrixCode?.ImageMimeType);
    }

    [Fact]
    public void Test_NewUserIdAndAllowedTanProcedures()
    {
        using var server = new FakeFinTsServer();
        var client = CreateClient(server, 6, "902");

        client.Parse_Segments("HNHBK:1:3+000000000100+300+DIALOG1+1'HIRMG:2:2+3060::Hinweise.'"
            + "HIRMS:3:2:3+3920::Zugelassene Zwei-Schritt-Verfahren für den Benutzer.:902:903+3072::Neue Benutzerkennung:1234567890'HNHBS:4:1+1'");

        Assert.Equal("1234567890", client.NewUserId);
        Assert.Equal(new[] { 902, 903 }, client.AllowedTanProcedures);
    }
}
