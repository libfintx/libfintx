using System;
using libfintx.FinTS;
using Xunit;

namespace libfintx.Tests;

public class Test_EncodeTo64
{
    [Fact]
    public void Umlaut_RoundTrips_As_Latin1()
    {
        var encoded = Helper.EncodeTo64("Alle Geräte");

        Assert.Equal("Alle Geräte", Helper.DecodeFrom64(encoded));
        Assert.Equal("Alle Geräte", Helper.DecodeFrom64EncodingDefault(encoded));
    }

    [Fact]
    public void Umlaut_Is_Encoded_As_Single_Latin1_Byte()
    {
        var bytes = Convert.FromBase64String(Helper.EncodeTo64("Alle Geräte"));

        // One byte per character: the FinTS length fields (HNHBK, @len@) are computed from string.Length.
        Assert.Equal("Alle Geräte".Length, bytes.Length);
        Assert.Equal(0xE4, bytes[8]);
    }
}
