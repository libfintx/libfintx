using libfintx.FinTS;
using Xunit;

namespace libfintx.Tests;

[Collection("UPD")]
public class Test_UpdAccountKind
{
    [Fact]
    public void TheAccountKind_IsKeptNextToTheProductName()
    {
        UPD.ParseUpd("HIUPD:165:6:4+0123456789::280:10050000+DE22100500000123456789+5985932562+10+EUR+Meier+Peter+Sparkassenbuch Gold+'"
            + "HIUPD:166:6:4+9876543210::280:10050000+DE11100500009876543210+5985932562+30+EUR+Meier+Peter+Depot+'");

        Assert.Equal(2, UPD.AccountList.Count);
        Assert.Equal(10, UPD.AccountList[0].AccountKind);
        Assert.Equal("Sparkassenbuch Gold", UPD.AccountList[0].AccountType);
        Assert.Equal(30, UPD.AccountList[1].AccountKind);
        Assert.Equal("Depot", UPD.AccountList[1].AccountType);
    }
}
