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
}
