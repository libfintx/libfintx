using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using libfintx.FinTS;
using libfintx.FinTS.Data;
using libfintx.FinTS.Message;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// HKTAN and HNSHA as sent when a TAN is submitted (process 2) or a decoupled approval is polled (process S).
/// Element positions as in "FinTS 3.0 Security - PIN/TAN" (2020-07-10), HKTAN#6 p. 58 f., HKTAN#7 p. 65:
/// 2 TAN process, 6 order reference, 7 "Weitere TAN folgt" (mandatory for process 2 and S), 12 TAN medium (optional).
/// Position = number of preceding data element separators + 1, the segment header being position 1.
/// </summary>
public class Test_HKTAN
{
    private const string OrderReference = "REF-4711";

    [Theory]
    [InlineData(6, "1234567", null, "2")]
    [InlineData(7, "1234567", null, "2")]
    [InlineData(7, null, null, "S")]
    [InlineData(6, "1234567", "Mobile", "2")]
    [InlineData(7, "1234567", "Mobile", "2")]
    public async Task Test_HKTAN_Process2AndS(int version, string tan, string tanMedium, string process)
    {
        var message = await CaptureAsync(client => client.TAN(tan), version, tanMedium);

        var hktan = Elements(message, "HKTAN");
        Assert.Equal($"HKTAN:3:{version}", hktan[0]);
        Assert.Equal(process, hktan[1]);           // 2: TAN process
        Assert.Equal(OrderReference, hktan[5]);    // 6: order reference
        Assert.Equal("N", hktan[6]);               // 7: Weitere TAN folgt
        Assert.Equal(7, hktan.Count);              // nothing after element 7, no TAN medium

        var hnsha = Elements(message, "HNSHA");
        Assert.Equal(tan == null ? "testpin" : "testpin:" + tan, hnsha[3]);
    }

    [Theory]
    [InlineData("902:1234567", "testpin:1234567")]
    [InlineData("902:4711", "testpin:4711")]                 // shorter than the former minimum of 10 characters
    [InlineData("902:12?3+4:5@6", "testpin:12??3?+4?:5?@6")] // syntax characters are escaped like in the PIN
    [InlineData("902", "testpin")]
    public void Test_TanInSignature(string procedureAndTan, string expectedSignature)
    {
        var client = CreateClient(0);
        client.SEGNUM = 3;

        var message = FinTSMessage.Create(client, 1, "0", "HKTAN:3:6+2++++" + OrderReference + "+N'", procedureAndTan);

        Assert.Equal(expectedSignature, Elements(message, "HNSHA")[3]);
    }

    private static FinTsClient CreateClient(int port)
    {
        var client = TestHelper.CreateTestClient(new ConnectionDetails
        {
            Url = $"http://127.0.0.1:{port}/",
            Blz = 12345678,
            UserId = "testuser",
            Pin = "testpin",
            CustomerSystemId = "0",
        });
        client.HIRMS = "902";
        return client;
    }

    /// <summary>Data elements of the first segment <paramref name="name"/> inside the encrypted part of a message.</summary>
    private static List<string> Elements(string message, string name)
    {
        var match = Regex.Match(message, @"HNVSD:\d+:\d+\+@(\d+)@");
        Assert.True(match.Success, "No HNVSD in message");
        var inner = message.Substring(match.Index + match.Length, int.Parse(match.Groups[1].Value));

        var segment = Helper.SplitSegments(inner).First(s => s.StartsWith(name + ":", StringComparison.Ordinal));
        if (segment.EndsWith("'", StringComparison.Ordinal) && !segment.EndsWith("?'", StringComparison.Ordinal))
            segment = segment.Substring(0, segment.Length - 1);

        return Helper.SplitDataElements(segment);
    }

    /// <summary>Lets the client send one message to a local listener and returns it as plain text.</summary>
    private static async Task<string> CaptureAsync(Func<FinTsClient, Task> action, int hktanVersion, string tanMedium)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var client = CreateClient(((IPEndPoint) listener.LocalEndpoint).Port);
            client.HITANS = hktanVersion;
            client.HITAB = tanMedium;
            client.HITAN = OrderReference;

            var receive = Task.Run(async () =>
            {
                using var connection = await listener.AcceptTcpClientAsync();
                using var stream = connection.GetStream();
                var buffer = new List<byte>();
                var block = new byte[8192];
                int headerEnd = -1, length = -1;
                while (headerEnd < 0 || buffer.Count < headerEnd + length)
                {
                    var read = await stream.ReadAsync(block, 0, block.Length);
                    if (read == 0)
                        break;

                    buffer.AddRange(block.Take(read));
                    var text = Encoding.ASCII.GetString(buffer.ToArray());
                    var end = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    if (headerEnd < 0 && end >= 0)
                    {
                        headerEnd = end + 4;
                        var header = text.Substring(0, end).Split("\r\n")
                            .First(l => l.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
                        length = int.Parse(header.Split(':')[1].Trim());
                    }
                }

                var body = Encoding.ASCII.GetString(buffer.ToArray(), headerEnd, length);
                var answer = Convert.ToBase64String(Encoding.Latin1.GetBytes(
                    "HNHBK:1:3+000000000100+300+DIALOG+1+DIALOG:1'HIRMG:2:2+0010::Nachricht entgegengenommen.'HNHBS:3:1+1'"));
                var response = "HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\n"
                    + $"Content-Length: {answer.Length}\r\nConnection: close\r\n\r\n{answer}";
                var bytes = Encoding.ASCII.GetBytes(response);
                await stream.WriteAsync(bytes, 0, bytes.Length);

                return Encoding.Latin1.GetString(Convert.FromBase64String(body));
            });

            try
            {
                await action(client);
            }
            catch (Exception)
            {
                // The answer is minimal; only the sent message is checked.
            }

            return await receive.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            listener.Stop();
        }
    }
}
