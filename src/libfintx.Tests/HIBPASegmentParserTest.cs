using libfintx.FinTS.Data.Segment;
using libfintx.FinTS.Segments;
using Xunit;

namespace libfintx.Tests;

public class HIBPASegmentParserTest
{
    [Theory]
    [InlineData("HIBPA:6:3:4+54+280:53070394+Postbank+0+1+300+9999", 54, 53070394)]
    [InlineData("HIBPA:5:3:3+39+280:10090000+Berliner Volksbank+1+1+300+1000", 39, 10090000)]
    [InlineData("HIBPA:6:3:4+2+280:76030080+Example Bank+0+1:2+201:210:220:300:400+100", 2, 76030080)]
    [InlineData("HIBPA:6:3:4+7+280:50010517+Example Bank+0+1+220:300+200", 7, 50010517)]
    public void ParsesBankParameterData(string rawSegment, int bpdVersion, int bankCode)
    {
        var segment = new GenericSegmentParser().ParseSegment(new Segment(rawSegment));

        var hibpa = (HIBPA) new HIBPASegmentParser().ParseSegment(segment);

        Assert.Equal(bpdVersion, hibpa.BpdVersion);
        Assert.Equal(280, hibpa.BankCountry);
        Assert.Equal(bankCode, hibpa.BankCode);
    }
}
