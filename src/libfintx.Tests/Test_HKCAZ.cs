using System.Threading.Tasks;
using libfintx.FinTS;
using libfintx.FinTS.BankParameterData;
using libfintx.FinTS.Camt;
using Xunit;

namespace libfintx.Tests;

[Collection("UPD")]
public class Test_HKCAZ
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "NEXT")]
    [InlineData("20261101", null)]
    [InlineData("20261101", "NEXT")]
    public async Task MissingFormat_UsesEscapedCamt08_InEveryRequestVariant(string fromDate, string startpoint)
    {
        foreach (var missingFormat in new[] { null, "", " " })
            await AssertRequestFormat(missingFormat, CamtScheme.Camt052_001_08, fromDate, startpoint);
    }

    [Theory]
    [InlineData(CamtScheme.Camt052_001_02)]
    [InlineData(CamtScheme.Camt052_001_08)]
    public async Task AdvertisedFormat_IsParsedAndPreserved(string format)
    {
        var client = TestHelper.CreateTestClient();
        client.Parse_Segments($"HICAZS:92:1:4+1+1+1+450:N:N:{format}'");
        Assert.Equal(format, client.HICAZS_Camt);

        await AssertRequestFormat(client.HICAZS_Camt, format, null, null);
        await AssertRequestFormat(client.HICAZS_Camt, format, null, "NEXT");
        await AssertRequestFormat(client.HICAZS_Camt, format, "20261101", null);
        await AssertRequestFormat(client.HICAZS_Camt, format, "20261101", "NEXT");
    }

    private static async Task AssertRequestFormat(string configured, string expected, string fromDate, string startpoint)
    {
        var message = await Test_HKTAN.CaptureAsync(async client =>
        {
            client.BPD = BPD.Parse("HIBPA:5:3:3+12+280:12345678+Testbank+0+1+300+500'", client.Logger);
            client.HICAZS_Camt = configured;
            await HKCAZ.Init_HKCAZ(client, fromDate, "20261116", startpoint, CamtVersion.Camt052);
        }, 7, null);

        Assert.Contains("+" + expected + "+N", message);
        var elements = Test_HKTAN.Elements(message, "HKCAZ");
        Assert.Equal(expected, elements[2]);
        Assert.Equal("N", elements[3]);
    }
}
