using System.Linq;
using libfintx.FinTS;
using Xunit;

namespace libfintx.Tests;

public class Test_EscapeHbciString
{
    [Theory]
    [InlineData("Handy", "Handy")]
    [InlineData("a:b+c'd", "a?:b?+c?'d")]
    [InlineData("a?b", "a??b")]
    [InlineData("a@b", "a?@b")]
    [InlineData("?:", "???:")]
    public void Escapes_Syntax_Characters(string value, string expected)
    {
        Assert.Equal(expected, Helper.EscapeHbciString(value));
    }

    [Fact]
    public void Null_Stays_Null()
    {
        Assert.Null(Helper.EscapeHbciString(null));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(7)]
    public void HKTAN_Escapes_TAN_Medium_Name(int hitans)
    {
        var client = TestHelper.CreateTestClient();
        client.HITANS = hitans;
        client.SEGNUM = 3;
        client.HITAB = "Handy+1:2";

        var segment = HKTAN.Init_HKTAN(client, string.Empty, "HKIDN");

        Assert.EndsWith("+Handy?+1?:2'", segment);
    }

    [Fact]
    public void Parsed_TAN_Medium_Name_Is_Escaped_Once_In_HKTAN()
    {
        var client = TestHelper.CreateTestClient();
        client.HITANS = 6;
        client.SEGNUM = 3;
        client.HITAB = FinTsClient.Parse_TANMedium("HITAB:4:4:3+0+M:1:::::::::::mT?:MFN1:********0340'").Single();

        var segment = HKTAN.Init_HKTAN(client, string.Empty, "HKIDN");

        Assert.EndsWith("+mT?:MFN1'", segment);
    }
}
