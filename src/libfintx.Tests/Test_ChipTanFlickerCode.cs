using System;
using libfintx.FinTS;
using Xunit;

namespace libfintx.Tests;

public class Test_ChipTanFlickerCode
{
    // HHD 1.4: LC 029, start code 8701 1234567, DE1 1234567890, DE2 1,00
    private const string HhdUc = "029" + "87" + "01" + "1234567" + "10" + "1234567890" + "04" + "1,00";

    [Fact]
    public void Reads_Code_From_Binary_HHD_UC_Element()
    {
        var hitan = $"HITAN:5:6:4+4++4711-0815+Bitte scannen Sie die Grafik.+@{HhdUc.Length}@{HhdUc}+20261003:135345";

        var code = Helper.ExtractChipTanFlickerCode(hitan);

        Assert.Equal(HhdUc, code);
        Assert.False(string.IsNullOrEmpty(new FlickerCode(code).Render()));
    }

    [Fact]
    public void Reads_Code_From_Challenge_Text()
    {
        var hitan = $"HITAN:5:6:4+4++4711-0815+CHLGUC 0028{HhdUc.Substring(1)}CHLGTEXT0030 Bitte scannen Sie die Grafik.";

        var code = Helper.ExtractChipTanFlickerCode(hitan);

        Assert.Equal(new FlickerCode(HhdUc), new FlickerCode(code));
    }

    [Theory]
    [InlineData("HITAN:5:6:4+4++4711-0815+Bitte geben Sie die TAN ein.")]
    [InlineData("HITAN:5:6:4+4++4711-0815+CHLGUC0000CHLGTEXT Bitte geben Sie die TAN ein.")]
    [InlineData("HITAN:5:6:4+4++4711-0815+Bitte geben Sie die TAN ein.+@0@")]
    [InlineData("")]
    [InlineData(null)]
    public void Returns_Null_Without_Flicker_Code(string hitan)
    {
        Assert.Null(Helper.ExtractChipTanFlickerCode(hitan));
    }

    [Fact]
    public void Empty_CHLGUC_Section_Is_A_Format_Error()
    {
        Assert.Throws<FormatException>(() => new FlickerCode("CHLGUCCHLGTEXT"));
    }
}
