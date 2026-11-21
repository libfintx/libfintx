using System.Threading.Tasks;
using libfintx.FinTS;
using libfintx.FinTS.BankParameterData;
using libfintx.FinTS.Camt;
using libfintx.FinTS.Data;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// The Kontoverbindung international (KTI) sent in HKSAL v7, HKCAZ and HKKAZ v7: IBAN and BIC when
/// the account has an IBAN, otherwise the national account.
/// </summary>
public class Test_KTI
{
    private static FinTsClient CreateClient(FakeFinTsServer server, bool withIban)
    {
        var client = TestHelper.CreateTestClient(new ConnectionDetails
        {
            Url = server.Url,
            Blz = 10070000,
            UserId = "user",
            Pin = "pin",
            Account = withIban ? "123456789" : "1099164406",
            SubAccount = withIban ? null : "01",
            Iban = withIban ? "DE02100700000123456789" : null,
            Bic = "DEUTDEBBXXX",
        });
        client.BPD = BPD.Parse("HIBPA:4:3:5+1+280:10070000+Fake Bank+0+1+300+9999'");
        client.HISALS = 7;
        client.HIKAZS = 7;
        client.HICAZS_Camt = CamtScheme.Camt052_001_02;
        return client;
    }

    [Fact]
    public void Test_National()
    {
        Assert.Equal("::1099164406:01:280:10070000", KTI.National("1099164406", "01", "10070000"));
        Assert.Equal("::1099164406::280:10070000", KTI.National("1099164406", null, "10070000"));
    }

    [Theory]
    [InlineData(true, "HKSAL:3:7+DE02100700000123456789:DEUTDEBBXXX+N'")]
    [InlineData(false, "HKSAL:3:7+::1099164406:01:280:10070000+N'")]
    public async Task Test_HKSAL(bool withIban, string expected)
    {
        using var server = new FakeFinTsServer();
        await HKSAL.Init_HKSAL(CreateClient(server, withIban));
        Assert.Equal(expected, server.Segment("HKSAL"));
    }

    [Theory]
    [InlineData(true, "HKCAZ:3:1+DE02100700000123456789:DEUTDEBBXXX:123456789::280:10070000+urn?:iso?:std?:iso?:20022?:tech?:xsd?:camt.052.001.02+N+20260901+20260930'")]
    [InlineData(false, "HKCAZ:3:1+::1099164406:01:280:10070000+urn?:iso?:std?:iso?:20022?:tech?:xsd?:camt.052.001.02+N+20260901+20260930'")]
    public async Task Test_HKCAZ(bool withIban, string expected)
    {
        using var server = new FakeFinTsServer();
        await HKCAZ.Init_HKCAZ(CreateClient(server, withIban), "20260901", "20260930", null, CamtVersion.Camt052);
        Assert.Equal(expected, server.Segment("HKCAZ"));
    }

    [Theory]
    [InlineData(true, "HKKAZ:3:7+DE02100700000123456789:DEUTDEBBXXX:123456789::280:10070000+N+20260901+20260930'")]
    [InlineData(false, "HKKAZ:3:7+::1099164406:01:280:10070000+N+20260901+20260930'")]
    public async Task Test_HKKAZ(bool withIban, string expected)
    {
        using var server = new FakeFinTsServer();
        await HKKAZ.Init_HKKAZ(CreateClient(server, withIban), "20260901", "20260930", null);
        Assert.Equal(expected, server.Segment("HKKAZ"));
    }

    [Theory]
    [InlineData(true, "HKKAZ:3:7+DE02100700000123456789:DEUTDEBBXXX:123456789::280:10070000+N++++4711'")]
    [InlineData(false, "HKKAZ:3:7+::1099164406:01:280:10070000+N++++4711'")]
    public async Task Test_HKKAZ_Startpoint(bool withIban, string expected)
    {
        using var server = new FakeFinTsServer();
        await HKKAZ.Init_HKKAZ(CreateClient(server, withIban), null, null, "4711");
        Assert.Equal(expected, server.Segment("HKKAZ"));
    }
}
