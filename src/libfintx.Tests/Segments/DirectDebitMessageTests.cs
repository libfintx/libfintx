using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using libfintx.FinTS;
using libfintx.FinTS.BankParameterData;
using libfintx.FinTS.Data;
using Xunit;

namespace libfintx.Tests.Segments;

/// <summary>
/// Captures the HKDSE message on a local endpoint and checks its segment numbers.
/// HKDSE used to be numbered 4 like the HKTAN that follows it, so segment 3 was missing
/// and 4 appeared twice.
/// </summary>
public class DirectDebitMessageTests
{
    private const string Xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Document xmlns=\"urn:iso:std:iso:20022:tech:xsd:pain.008.001.08\"><CstmrDrctDbtInitn/></Document>";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Init_HKDSE_WithPayload_NumbersSegmentsConsecutively(bool tanRequired)
    {
        var inner = await CaptureInnerSegments(tanRequired,
            client => HKDSE.Init_HKDSE(client, Xml, 60.10m, "pain.008.001.08"));

        AssertConsecutive(inner, tanRequired);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Init_HKDSE_NumbersSegmentsConsecutively(bool tanRequired)
    {
        var inner = await CaptureInnerSegments(tanRequired,
            client => HKDSE.Init_HKDSE(client, "Payer", "DE02120300000000202051", "BYLADEM1001", 60.10m,
                "Invoice 1", new DateTime(2026, 10, 6), "M-1", new DateTime(2026, 1, 1), "DE98ZZZ09999999999"));

        AssertConsecutive(inner, tanRequired);
    }

    private static void AssertConsecutive(List<(string Name, int Number)> inner, bool tanRequired)
    {
        var expected = tanRequired
            ? new[] { ("HNSHK", 2), ("HKDSE", 3), ("HKTAN", 4), ("HNSHA", 5) }
            : new[] { ("HNSHK", 2), ("HKDSE", 3), ("HNSHA", 4) };

        Assert.Equal(expected, inner);
    }

    private static async Task<List<(string Name, int Number)>> CaptureInnerSegments(bool tanRequired, Func<FinTsClient, Task<string>> send)
    {
        var port = FreePort();
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var bpdDir = Directory.CreateTempSubdirectory("libfintx-bpd-");
        try
        {
            File.WriteAllText(Path.Combine(bpdDir.FullName, "12030000.bpd"),
                "HIBPA:5:3:4+78+280:12030000+Bank+0+1+300+9999'" +
                "HIPINS:7:1:4+1+1+0+5:50:6:::HKDSE:" + (tanRequired ? "J" : "N") + "'" +
                "HIDSES:43:1:4+1+1+0+1:90:1:90'");

            var capture = Task.Run(async () =>
            {
                var context = await listener.GetContextAsync();
                string body;
                using (var reader = new StreamReader(context.Request.InputStream, Encoding.ASCII))
                    body = reader.ReadToEnd();

                var answer = Convert.ToBase64String(Encoding.ASCII.GetBytes(
                    "HNHBK:1:3+000000000000+300+DIALOG+2'HIRMG:2:2+0010::OK'HNHBS:3:1+2'"));
                var bytes = Encoding.ASCII.GetBytes(answer);
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();

                return Encoding.ASCII.GetString(Convert.FromBase64String(body));
            });

            var client = new FinTsClient(new ConnectionDetails
            {
                Url = $"http://127.0.0.1:{port}/",
                Blz = 12030000,
                UserId = "user",
                Pin = "pin",
                Iban = "DE89370400440532013000",
                Bic = "COBADEFFXXX",
                AccountHolder = "Creditor",
                CustomerSystemId = "system",
            }, false, new BpdFileStore(bpdDir.FullName))
            {
                HIRMS = "920",
                HITANS = 6,
                HITAB = "medium",
            };
            client.HNHBK = "DIALOG";
            client.HNHBS = 2;

            try { await send(client); } catch (Exception) { /* the canned answer is not a full bank message */ }

            var message = await capture;
            var start = message.IndexOf("HNVSD:999:1+@", StringComparison.Ordinal) + "HNVSD:999:1+@".Length;
            var lengthEnd = message.IndexOf('@', start);
            var length = int.Parse(message.Substring(start, lengthEnd - start));
            var encrypted = message.Substring(lengthEnd + 1, length);

            return SegmentHeads(encrypted);
        }
        finally
        {
            listener.Stop();
            bpdDir.Delete(true);
        }
    }

    /// <summary>Segment name and number of each segment, skipping binary data.</summary>
    private static List<(string Name, int Number)> SegmentHeads(string segments)
    {
        var heads = new List<(string, int)>();
        var i = 0;
        while (i < segments.Length)
        {
            var head = Regex.Match(segments.Substring(i), @"^([A-Z]{5,6}):(\d+):");
            Assert.True(head.Success, "No segment head at " + i + ": " + segments.Substring(i, Math.Min(20, segments.Length - i)));
            heads.Add((head.Groups[1].Value, int.Parse(head.Groups[2].Value)));

            // Walk to the unescaped segment terminator, jumping over @len@ binary data.
            while (i < segments.Length && segments[i] != '\'')
            {
                if (segments[i] == '?') { i += 2; continue; }
                if (segments[i] == '@')
                {
                    var end = segments.IndexOf('@', i + 1);
                    var n = int.Parse(segments.Substring(i + 1, end - i - 1));
                    i = end + 1 + n;
                    continue;
                }
                i++;
            }
            i++;
        }
        return heads;
    }

    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint) probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}
