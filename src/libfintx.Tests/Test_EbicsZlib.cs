using System;
using System.Linq;
using System.Text;
using libfintx.EBICS;
using Xunit;

namespace libfintx.Tests;

public class Test_EbicsZlib
{
    private static readonly string Document =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Document>" + string.Concat(Enumerable.Repeat("Grüße ", 50)) + "</Document>";

    [Fact]
    public void DecompressesZlibDataFromAnotherImplementation()
    {
        // Compressed with Python's zlib (level 9), as a bank sends order data.
        var compressed = Convert.FromBase64String("eNqzsa/IzVEoSy0qzszPs1Uy1DNQUkjNS85PycxLt1UKDXHTtVCyt7NxyU8uzU3NK7FzLzq85/D8VIVRenDQNvrwqAEA1erhaw==");

        Assert.Equal(Document, Encoding.UTF8.GetString(Zlib.Decompress(compressed)));
    }

    [Fact]
    public void CompressesToZlibThatDecompressesAgain()
    {
        var data = Encoding.UTF8.GetBytes(Document);

        var compressed = Zlib.Compress(data);

        Assert.Equal(0x78, compressed[0]); // zlib header (RFC 1950), not raw deflate or gzip
        Assert.Equal(0, ((compressed[0] << 8) | compressed[1]) % 31);
        Assert.True(compressed.Length < data.Length);
        Assert.Equal(data, Zlib.Decompress(compressed));
    }

    [Fact]
    public void HandlesEmptyData()
    {
        Assert.Empty(Zlib.Decompress(Zlib.Compress(Array.Empty<byte>())));
    }
}
