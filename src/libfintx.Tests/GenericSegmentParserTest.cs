using libfintx.FinTS.Data.Segment;
using Xunit;

namespace libfintx.Tests;

public class GenericSegmentParserTest
{
    [Fact]
    public void TestHikazs()
    {
        var segmentCode = "HIKAZS:23:6:4+20+1+1+90:N:N";
        var segment = new Segment(segmentCode);
        segment = new GenericSegmentParser().ParseSegment(segment);

        Assert.Equal("HIKAZS", segment.Name);
        Assert.Equal(23, segment.Number);
        Assert.Equal(6, segment.Version);
        Assert.Equal(4, segment.Ref);
        Assert.False(string.IsNullOrWhiteSpace(segment.Payload));
    }

    [Fact]
    public void TestHitan()
    {
        var segmentCode = "HITAN:5:7:3+S++eNmo9/2dEocBAACRO?+gjhW?+owAQA";
        var segment = new Segment(segmentCode);
        segment = new GenericSegmentParser().ParseSegment(segment);

        Assert.Equal("HITAN", segment.Name);
        Assert.Equal(5, segment.Number);
        Assert.Equal(7, segment.Version);
        Assert.Equal(3, segment.Ref);
        Assert.Equal(3, segment.DataElements.Count);
        Assert.Equal("eNmo9/2dEocBAACRO?+gjhW?+owAQA", segment.DataElements[2]);
    }

    [Fact]
    public void TestHicazWithBinaryCamt()
    {
        const string camt = "<?xml version=\"1.0\"?><Document><Ustrd>A+B?C'D</Ustrd></Document>";
        var segmentCode = $"HICAZ:5:1:3+DE02120300000000202051:BYLADEM1001+urn?:iso?:std?:iso?:20022?:tech?:xsd?:camt.052.001.08+@{camt.Length}@{camt}";
        var segment = new GenericSegmentParser().ParseSegment(new Segment(segmentCode));

        Assert.Equal("HICAZ", segment.Name);
        Assert.Equal(3, segment.DataElements.Count);
        Assert.Equal($"@{camt.Length}@{camt}", segment.DataElements[2]);
    }

    [Fact]
    public void TestEmptyReference()
    {
        var segment = new Segment("HIRMG:2:2:+0010::Nachricht entgegengenommen.");
        segment = new GenericSegmentParser().ParseSegment(segment);

        Assert.Equal("HIRMG", segment.Name);
        Assert.Equal(2, segment.Number);
        Assert.Equal(2, segment.Version);
        Assert.Null(segment.Ref);
        Assert.Equal("0010::Nachricht entgegengenommen.", segment.Payload);
    }
}
