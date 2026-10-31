using libfintx.FinTS.Message;
using Xunit;

namespace libfintx.Tests;

public class Test_TraceMasking
{
    [Theory]
    [InlineData("HNSHA:5:2+1234567++secret'", "HNSHA:5:2+1234567++***'")]
    [InlineData("HNSHA:5:2+1234567++secret:123456'", "HNSHA:5:2+1234567++***'")]
    [InlineData("HNSHA:5:2+1234567++?+se?'cr?:et:123456'HNHBS:6:1+1'", "HNSHA:5:2+1234567++***'HNHBS:6:1+1'")]
    [InlineData("HNHBS:6:1+1'", "HNHBS:6:1+1'")]
    public void PIN_And_TAN_In_HNSHA_Are_Masked(string message, string expected)
    {
        Assert.Equal(expected, FinTSMessage.MaskUserTan(message, string.Empty));
    }

    [Fact]
    public void Escaped_User_Id_Is_Masked_Wherever_It_Appears()
    {
        // The old \b-anchored pattern missed a user id that starts with an escape character
        var masked = FinTSMessage.MaskUserTan("HKIDN:3:2+280:12345678+?+user1+0+1'HNSHK:2:4+PIN:2+942+1+1+1::0+1+1:20260101:000000+1:999:1+6:10:16+280:12345678:?+user1:S:0:0'", "?+user1");

        Assert.DoesNotContain("user1", masked);
    }

    [Fact]
    public void Empty_User_Id_Leaves_The_Message_Unchanged()
    {
        Assert.Equal("HKIDN:3:2+280:12345678+user+0+1'", FinTSMessage.MaskUserTan("HKIDN:3:2+280:12345678+user+0+1'", string.Empty));
    }

    [Theory]
    [InlineData("HKPAE:3:1+4711+0815'", "4711", "HKPAE:3:1+XXXX+0815'")]
    [InlineData("HKPAE:3:1+se?+cret+0815'", "se?+cret", "HKPAE:3:1+XXXXXXXX+0815'")]
    [InlineData("HKPAE:3:1+0815:4711'", "4711", "HKPAE:3:1+0815:XXXX'")]
    public void PIN_Is_Masked_Where_It_Is_A_Whole_Data_Element(string message, string pin, string expected)
    {
        Assert.Equal(expected, FinTSMessage.MaskUserTan(message, string.Empty, pin));
    }

    [Theory]
    [InlineData("HIUPD:7:6:4+1234711560::280:12345678'", "4711")] // inside another value
    [InlineData("HKPAE:3:1+4711?+0815'", "4711")]                   // element continues after an escape
    public void PIN_Is_Not_Replaced_Inside_Other_Values(string message, string pin)
    {
        Assert.Equal(message, FinTSMessage.MaskUserTan(message, string.Empty, pin));
    }
}
