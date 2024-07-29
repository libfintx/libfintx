using System;
using System.Text.RegularExpressions;
using libfintx.FinTS.Segments;

namespace libfintx.FinTS.Data.Segment;

internal class HIBPASegmentParser : ISegmentParser
{
    public Segment ParseSegment(Segment segment)
    {
        var result = new HIBPA(segment);

        // HIBPA:5:3:3+39+280:10090000+Berliner Volksbank+1+1+300+1000'
        // HIBPA:6:3:4+54+280:53070394+Postbank+0+1+300+9999
        // payload: 54+280:53070394+Postbank+0+1+300+9999
        var match = Regex.Match(segment.Payload, @"^(?<bdpversion>\d*)\+(?<bankcountry>\d*):(?<bankcode>\d*)\+(?<bankname>.*)\+(?<notransactions>\d*)\+(?<supportedLanguages>\d*)\+(?<maxMessageSize>\d+)?");
        if (!match.Success)
            throw new ArgumentException($"Could not parse segment{Environment.NewLine}{segment.Payload}");

        result.BpdVersion = int.Parse(match.Groups["bdpversion"].Value);
        result.BankCountry = int.Parse(match.Groups["bankcountry"].Value);
        result.BankCode = int.Parse(match.Groups["bankcode"].Value);

        // rest is currently not interpreted

        return result;
    }
}
