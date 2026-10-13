using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace libfintx.Tests;

/// <summary>
/// Local FinTS endpoint for tests: records every request (decoded to plain FinTS text) and answers
/// each one with the same plain response.
/// </summary>
internal sealed class FakeFinTsServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly string _response;

    public FakeFinTsServer(string response = "HNHBK:1:3+000000000100+300+DIALOG1+1'HIRMG:2:2+0010::Nachricht entgegengenommen.'HNHBS:3:1+1'")
    {
        _response = response;
        _listener.Start();
        Url = $"http://127.0.0.1:{((IPEndPoint) _listener.LocalEndpoint).Port}/";
        _ = ServeAsync();
    }

    public string Url { get; }

    public List<string> Requests { get; } = new();

    /// <summary>The first segment with the given name that was sent, including its terminator.</summary>
    public string Segment(string name) =>
        Requests.Select(r => Regex.Match(r, name + @":\d+:\d+(\+[^']*)?'")).First(m => m.Success).Value;

    private async Task ServeAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync();
            }
            catch (Exception)
            {
                return;
            }

            using (client)
            {
                var stream = client.GetStream();
                var request = await ReadBodyAsync(stream);
                lock (Requests)
                    Requests.Add(Encoding.GetEncoding("ISO-8859-1").GetString(Convert.FromBase64String(request)));

                var body = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(_response));
                var http = "HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\n"
                           + $"Content-Length: {body.Length}\r\nConnection: close\r\n\r\n{body}";
                var bytes = Encoding.ASCII.GetBytes(http);
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }
    }

    private static async Task<string> ReadBodyAsync(NetworkStream stream)
    {
        var buffer = new List<byte>();
        var chunk = new byte[4096];
        int headerEnd = -1, contentLength = 0;
        while (headerEnd < 0 || buffer.Count < headerEnd + contentLength)
        {
            var n = await stream.ReadAsync(chunk, 0, chunk.Length);
            if (n == 0)
                break;
            buffer.AddRange(chunk.Take(n));
            if (headerEnd >= 0)
                continue;
            var text = Encoding.ASCII.GetString(buffer.ToArray());
            var idx = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (idx < 0)
                continue;
            headerEnd = idx + 4;
            var match = Regex.Match(text.Substring(0, idx), @"Content-Length:\s*(\d+)", RegexOptions.IgnoreCase);
            contentLength = match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }
        return Encoding.ASCII.GetString(buffer.ToArray(), headerEnd, buffer.Count - headerEnd);
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
        _stop.Dispose();
    }
}
