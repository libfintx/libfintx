using System.Linq;
using System.Threading.Tasks;
using libfintx.FinTS;
using libfintx.FinTS.BankParameterData;
using libfintx.FinTS.Data;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// HKSPA (SEPA-Kontoverbindung anfordern) and its answer HISPA.
/// </summary>
public class Test_HKSPA
{
    private const string Hispa =
        "HISPA:4:1:3+J:DE89100700000099164406:DEUTDEFFXXX:0099164406:01:280:10070000"
        + "+N:::7000123::280:10070000"
        + "+J:DE02100700000123456789:DEUTDEBB?:X:123456789::280:10070000";

    private static FinTsClient CreateClient(FakeFinTsServer server, int hispas)
    {
        var client = TestHelper.CreateTestClient(new ConnectionDetails
        {
            Url = server.Url,
            Blz = 10070000,
            UserId = "user",
            Pin = "pin",
            CustomerSystemId = "SYS",
        });
        client.BPD = BPD.Parse("HIBPA:4:3:5+1+280:10070000+Fake Bank+0+1+300+9999'");
        client.HISPAS = hispas;
        return client;
    }

    [Fact]
    public async Task Test_Segment()
    {
        using var server = new FakeFinTsServer();
        await HKSPA.Init_HKSPA(CreateClient(server, 1));
        Assert.Equal("HKSPA:3:1'", server.Segment("HKSPA"));
    }

    [Fact]
    public void Test_Parse_SepaAccounts()
    {
        var result = FinTsClient.Parse_SepaAccounts("HIRMG:2:2+0010::Nachricht entgegengenommen.'" + Hispa + "'");

        Assert.Equal(2, result.Count);
        Assert.Equal("DE89100700000099164406", result[0].AccountIban);
        Assert.Equal("DEUTDEFFXXX", result[0].AccountBic);
        Assert.Equal("0099164406", result[0].AccountNumber);
        Assert.Equal("01", result[0].SubAccountFeature);
        Assert.Equal("10070000", result[0].AccountBankCode);
        // non-SEPA account skipped, FinTS escaping removed
        Assert.Equal("DEUTDEBB:X", result[1].AccountBic);
        Assert.Equal("", result[1].SubAccountFeature);
    }

    [Fact]
    public async Task Test_SepaAccounts()
    {
        using var server = new FakeFinTsServer(
            "HNHBK:1:3+000000000100+300+DIALOG1+1'HIRMG:2:2+0010::Nachricht entgegengenommen.'" + Hispa + "'HNHBS:5:1+1'");

        var result = await CreateClient(server, 1).SepaAccounts(null);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "DE89100700000099164406", "DE02100700000123456789" }, result.Data.Select(a => a.AccountIban));
        Assert.Equal("HKSPA:3:1'", server.Segment("HKSPA"));
    }
}
