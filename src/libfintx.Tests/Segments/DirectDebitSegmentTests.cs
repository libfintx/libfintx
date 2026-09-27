using System;
using libfintx.FinTS;
using Xunit;

namespace libfintx.Tests.Segments;

public class DirectDebitSegmentTests
{
    private const string Segment = "HKDSE:4:1+DE89370400440532013000:COBADEFFXXX+urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.008.001.08+@@";

    private const string Xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Document>\n  <Nm>O'Brien</Nm>\n</Document>";

    [Fact]
    public void AttachPayload_AnnouncesTheFullLengthAndTerminatesTheSegment()
    {
        var result = DirectDebitSegment.AttachPayload(Segment, Xml);

        Assert.Equal(
            Segment.Substring(0, Segment.Length - 2) + "@" + Xml.Length + "@" + Xml + "'",
            result);
    }

    [Fact]
    public void AttachPayload_BinaryDataEndsWithTheLastCharacterOfTheXml()
    {
        var result = DirectDebitSegment.AttachPayload(Segment, Xml);

        var head = Segment.Substring(0, Segment.Length - 2) + "@";
        var lengthEnd = result.IndexOf('@', head.Length);
        var length = int.Parse(result.Substring(head.Length, lengthEnd - head.Length));
        var binary = result.Substring(lengthEnd + 1, length);

        Assert.Equal(Xml, binary);
        Assert.Equal("'", result.Substring(lengthEnd + 1 + length));
    }

    [Fact]
    public void AttachPayload_AcceptsAMessageThatAlreadyEndsWithTheTerminator()
    {
        // pain00800202.Create ends its message with "'".
        var result = DirectDebitSegment.AttachPayload(Segment, Xml + "'");

        Assert.Equal(DirectDebitSegment.AttachPayload(Segment, Xml), result);
    }

    [Fact]
    public void AttachPayload_RejectsASegmentWithoutPlaceholder()
    {
        Assert.Throws<ArgumentException>(() => DirectDebitSegment.AttachPayload("HKDSE:4:1+x+", Xml));
    }

    [Fact]
    public void AttachPayload_RejectsAnEmptyMessage()
    {
        Assert.Throws<ArgumentException>(() => DirectDebitSegment.AttachPayload(Segment, " "));
    }
}
