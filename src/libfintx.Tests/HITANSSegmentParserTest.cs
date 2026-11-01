using System.Linq;
using libfintx.FinTS;
using libfintx.FinTS.BankParameterData;
using libfintx.FinTS.Data.Segment;
using Xunit;

namespace libfintx.Tests;

public class HITANSSegmentParserTest
{
    [Fact]
    public void Test_HITANS_6()
    {
        var rawSegment = @"HITANS:174:6:4+1+1+1+J:N:0:910:2:HHD1.3.0:::chipTAN manuell:6:1:TAN-Nummer:3:J:2:N:0:0:N:N:00:0:N:1:911:2:HHD1.3.2OPT:HHDOPT1:1.3.2:chipTAN optisch:6:1:TAN-Nummer:3:J:2:N:0:0:N:N:00:0:N:1:912:2:HHD1.3.2USB:HHDUSB1:1.3.2:chipTAN-USB:6:1:TAN-Nummer:3:J:2:N:0:0:N:N:00:0:N:1:913:2:Q1S:Secoder_UC:1.2.0:chipTAN-QR:6:1:TAN-Nummer:3:J:2:N:0:0:N:N:00:0:N:1:920:2:smsTAN:::smsTAN:6:1:TAN-Nummer:3:J:2:N:0:0:N:N:00:2:N:5:921:2:pushTAN:::pushTAN:6:1:TAN-Nummer:3:J:2:N:0:0:N:N:00:2:N:2:900:2:iTAN:::iTAN:6:1:TAN-Nummer:3:J:2:N:0:0:N:N:00:0:N:0'";
        var segment = new Segment(rawSegment);
        segment = new GenericSegmentParser().ParseSegment(segment);

        var parser = new HITANSSegmentParser();
        var hitans = (HITANS) parser.ParseSegment(segment);

        Assert.Equal("HITANS", hitans.Name);
        Assert.Equal(7, hitans.TanProcesses.Count);
        Assert.Equal(910, hitans.TanProcesses[0].TanCode);
        Assert.Equal("chipTAN manuell", hitans.TanProcesses[0].Name);
        Assert.Equal(911, hitans.TanProcesses[1].TanCode);
        Assert.Equal("chipTAN optisch", hitans.TanProcesses[1].Name);
        Assert.Equal(912, hitans.TanProcesses[2].TanCode);
        Assert.Equal("chipTAN-USB", hitans.TanProcesses[2].Name);
        Assert.Equal(913, hitans.TanProcesses[3].TanCode);
        Assert.Equal("chipTAN-QR", hitans.TanProcesses[3].Name);
        Assert.Equal(920, hitans.TanProcesses[4].TanCode);
        Assert.Equal("smsTAN", hitans.TanProcesses[4].Name);
        Assert.Equal(921, hitans.TanProcesses[5].TanCode);
        Assert.Equal("pushTAN", hitans.TanProcesses[5].Name);
        Assert.Equal(900, hitans.TanProcesses[6].TanCode);
        Assert.Equal("iTAN", hitans.TanProcesses[6].Name);
    }

    [Fact]
    public void Test_HITANS_7()
    {
        var rawSegment = @"HITANS:175:7:4+1+1+1+N:N:0:922:2:pushTAN-dec:Decoupled::pushTAN 2.0:::Aufforderung:2048:J:2:N:0:0:N:N:00:2:N:2:180:1:1:J:J'";
        var segment = new Segment(rawSegment);
        segment = new GenericSegmentParser().ParseSegment(segment);

        var parser = new HITANSSegmentParser();
        var hitans = (HITANS) parser.ParseSegment(segment);

        Assert.Equal("HITANS", hitans.Name);
        Assert.Single(hitans.TanProcesses);
        Assert.Equal(922, hitans.TanProcesses[0].TanCode);
        Assert.Equal("pushTAN 2.0", hitans.TanProcesses[0].Name);
    }

    [Fact]
    public void Test_HITANS_7_DecoupledParameters()
    {
        // Elements 22-26 of the process: 180 status requests, 1 s before the first, 1 s before each further one,
        // manual confirmation allowed, automated status requests allowed
        var hitans = Parse(@"HITANS:175:7:4+1+1+1+N:N:0:922:2:pushTAN-dec:Decoupled::pushTAN 2.0:::Aufforderung:2048:J:2:N:0:0:N:N:00:2:N:2:180:1:1:J:J'");

        var tanProcess = Assert.Single(hitans.TanProcesses);
        Assert.Equal(180, tanProcess.DecoupledMaxStatusRequests);
        Assert.Equal(1, tanProcess.DecoupledWaitBeforeFirstStatusRequest);
        Assert.Equal(1, tanProcess.DecoupledWaitBeforeNextStatusRequest);
        Assert.True(tanProcess.DecoupledManualConfirmationAllowed);
        Assert.True(tanProcess.DecoupledAutomatedStatusRequestsAllowed);
    }

    [Fact]
    public void Test_HITANS_7_DecoupledParameters_SeveralProcesses()
    {
        // First process not decoupled (elements 22-26 empty, escaped ':' in its name), second process decoupled with
        // the optional elements 25 and 26 omitted at the end of the group
        var hitans = Parse(@"HITANS:175:7:4+1+1+1+N:N:0:"
            + "921:2:pushTAN:::pushTAN ?: mobile:::Aufforderung:2048:J:2:N:0:0:N:N:00:2:N:2::::::"
            + "922:2:pushTAN-dec:Decoupled::pushTAN 2.0:::Aufforderung:2048:J:2:N:0:0:N:N:00:2:N:2:60:5:3'");

        Assert.Equal(new[] { 921, 922 }, hitans.TanProcesses.Select(p => p.TanCode));
        var notDecoupled = hitans.TanProcesses[0];
        Assert.Null(notDecoupled.DecoupledMaxStatusRequests);
        Assert.Null(notDecoupled.DecoupledWaitBeforeFirstStatusRequest);
        Assert.Null(notDecoupled.DecoupledAutomatedStatusRequestsAllowed);
        var decoupled = hitans.TanProcesses[1];
        Assert.Equal(60, decoupled.DecoupledMaxStatusRequests);
        Assert.Equal(5, decoupled.DecoupledWaitBeforeFirstStatusRequest);
        Assert.Equal(3, decoupled.DecoupledWaitBeforeNextStatusRequest);
        Assert.Null(decoupled.DecoupledManualConfirmationAllowed);
        Assert.Null(decoupled.DecoupledAutomatedStatusRequestsAllowed);
    }

    [Fact]
    public void Test_HITANS_6_HasNoDecoupledParameters()
    {
        var hitans = Parse(@"HITANS:174:6:4+1+1+1+J:N:0:921:2:pushTAN:::pushTAN:6:1:TAN-Nummer:3:J:2:N:0:0:N:N:00:2:N:2'");

        Assert.Null(Assert.Single(hitans.TanProcesses).DecoupledMaxStatusRequests);
    }

    [Fact]
    public void Test_GetTanProcess()
    {
        var client = TestHelper.CreateTestClient();
        client.BPD = BPD.Parse("HIBPA:5:3:3+12+280:12345678+Testbank+0+1+300+500'"
            + "HITANS:174:6:4+1+1+1+J:N:0:921:2:pushTAN:::pushTAN:6:1:TAN-Nummer:3:J:2:N:0:0:N:N:00:2:N:2'"
            + "HITANS:175:7:4+1+1+1+N:N:0:922:2:pushTAN-dec:Decoupled::pushTAN 2.0:::Aufforderung:2048:J:2:N:0:0:N:N:00:2:N:2:180:1:1:J:J'");

        client.HIRMS = "922";
        Assert.Equal(180, client.GetTanProcess()?.DecoupledMaxStatusRequests);

        client.HIRMS = "921";
        Assert.Equal(921, client.GetTanProcess()?.TanCode);

        client.HIRMS = "999";
        Assert.Null(client.GetTanProcess());
    }

    private static HITANS Parse(string rawSegment)
    {
        var segment = new GenericSegmentParser().ParseSegment(new Segment(rawSegment));
        return (HITANS) new HITANSSegmentParser().ParseSegment(segment);
    }
}
