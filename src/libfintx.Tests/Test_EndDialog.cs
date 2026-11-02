using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using libfintx.FinTS;
using libfintx.FinTS.Data;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// HKEND ends the dialog the last business transaction opened (FinTS 3.0 Formals, C.4):
/// HKEND:3:1+dialog id', sent in a message that carries the same dialog id in HNHBK.
/// </summary>
public class Test_EndDialog
{
    private const string DialogId = "DIALOG-4711";

    [Fact]
    public async Task Test_SendsHkendWithTheDialogId()
    {
        var (messages, result) = await RunAsync(client => client.HNHBK = DialogId);

        var message = Assert.Single(messages);
        Assert.Equal(DialogId, Helper.SplitDataElements(Segment(message, "HNHBK"))[3]);
        Assert.Equal("HKEND:3:1+" + DialogId, Segment(Inner(message), "HKEND"));
        Assert.True(result.IsSuccess);
        Assert.Equal("0100", result.Messages.First(m => m.IsSuccess).Code);
    }

    [Fact]
    public async Task Test_EndsTheDialogOnlyOnce()
    {
        var client = default(FinTsClient);
        var (messages, second) = await RunAsync(async c =>
        {
            client = c;
            c.HNHBK = DialogId;
            await c.EndDialog();
        });

        Assert.Single(messages);
        Assert.Null(client.HNHBK);
        Assert.Empty(second.Messages);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    public async Task Test_WithoutOpenDialogSendsNothing(string dialogId)
    {
        var (messages, result) = await RunAsync(client => client.HNHBK = dialogId);

        Assert.Empty(messages);
        Assert.Empty(result.Messages);
    }

    private static Task<(List<string> Messages, HBCIDialogResult Result)> RunAsync(Action<FinTsClient> prepare) =>
        RunAsync(client =>
        {
            prepare(client);
            return Task.CompletedTask;
        });

    /// <summary>
    /// Prepares a client against a local listener, calls <see cref="FinTsClient.EndDialog"/> and returns the
    /// messages the listener received as plain text.
    /// </summary>
    private static async Task<(List<string> Messages, HBCIDialogResult Result)> RunAsync(Func<FinTsClient, Task> prepare)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var messages = new List<string>();
        using var stop = new CancellationTokenSource();
        var server = Task.Run(async () =>
        {
            while (!stop.IsCancellationRequested)
            {
                TcpClient connection;
                try
                {
                    connection = await listener.AcceptTcpClientAsync(stop.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                using (connection)
                {
                    var message = await AnswerAsync(connection.GetStream());
                    lock (messages)
                        messages.Add(message);
                }
            }
        });

        try
        {
            var client = TestHelper.CreateTestClient(new ConnectionDetails
            {
                Url = $"http://127.0.0.1:{((IPEndPoint) listener.LocalEndpoint).Port}/",
                Blz = 12345678,
                UserId = "testuser",
                Pin = "testpin",
                CustomerSystemId = "0",
            });
            client.HIRMS = "902";
            client.HNHBS = 3;

            await prepare(client);
            var result = await client.EndDialog();
            lock (messages)
                return (messages.ToList(), result);
        }
        finally
        {
            // The server leaves its loop before the listener stops, so that no accept meets a stopped listener
            stop.Cancel();
            await server;
            listener.Stop();
        }
    }

    private static async Task<string> AnswerAsync(NetworkStream stream)
    {
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
            $"HNHBK:1:3+000000000100+300+{DialogId}+4+{DialogId}:4'HIRMG:2:2+0100::Dialog beendet.'HNHBS:3:1+4'"));
        var response = "HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\n"
            + $"Content-Length: {answer.Length}\r\nConnection: close\r\n\r\n{answer}";
        var bytes = Encoding.ASCII.GetBytes(response);
        await stream.WriteAsync(bytes, 0, bytes.Length);

        return Encoding.Latin1.GetString(Convert.FromBase64String(body));
    }

    /// <summary>The plain text inside HNVSD.</summary>
    private static string Inner(string message)
    {
        var match = Regex.Match(message, @"HNVSD:\d+:\d+\+@(\d+)@");
        Assert.True(match.Success, "No HNVSD in message");
        return message.Substring(match.Index + match.Length, int.Parse(match.Groups[1].Value));
    }

    /// <summary>The first segment <paramref name="name"/>, without its terminator.</summary>
    private static string Segment(string text, string name)
    {
        var segment = Helper.SplitSegments(text).First(s => s.StartsWith(name + ":", StringComparison.Ordinal));
        if (segment.EndsWith("'", StringComparison.Ordinal) && !segment.EndsWith("?'", StringComparison.Ordinal))
            segment = segment.Substring(0, segment.Length - 1);

        return segment;
    }
}
