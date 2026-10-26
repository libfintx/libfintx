using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using libfintx.FinTS;
using libfintx.FinTS.BankParameterData;
using libfintx.FinTS.Data;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// Status requests (HKTAN process S) of a decoupled approval follow the wait times and the maximum number of requests
/// announced in HITANS#7 (FinTS 3.0 Security - PIN/TAN 2020-07-10, pp. 174 f.). A local listener answers every request
/// with 3956 (approval still pending).
/// </summary>
public class Test_DecoupledStatusRequests
{
    [Fact]
    public async Task Test_StopsAtMaximumNumberOfStatusRequests()
    {
        // Process 922: at most 2 status requests, 1 s before the first and before each further one
        const string bpd = "HIBPA:5:3:3+12+280:12345678+Testbank+0+1+300+500'"
            + "HITANS:175:7:4+1+1+1+N:N:0:922:2:pushTAN-dec:Decoupled::pushTAN 2.0:::Aufforderung:2048:J:2:N:0:0:N:N:00:2:N:2:2:1:1:J:J'";

        var (requests, elapsed) = await RunApprovalAsync(bpd);

        Assert.Equal(2, requests);
        Assert.True(elapsed < TimeSpan.FromSeconds(6), $"Polling went on for {elapsed}");
    }

    [Fact]
    public async Task Test_WithoutParametersPollsUntilCancelled()
    {
        // No decoupled parameters: as before, every 2 seconds without limit until the caller cancels
        const string bpd = "HIBPA:5:3:3+12+280:12345678+Testbank+0+1+300+500'";

        var (requests, _) = await RunApprovalAsync(bpd);

        Assert.True(requests >= 3, $"Only {requests} status requests");
    }

    private static async Task<(int Requests, TimeSpan Elapsed)> RunApprovalAsync(string bpd)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var requests = 0;
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
                    await AnswerAsync(connection.GetStream());
                    Interlocked.Increment(ref requests);
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
            client.BPD = BPD.Parse(bpd);
            client.HIRMS = "922";
            client.HITANS = 7;
            client.HITAN = "REF-4711";

            var dialog = new TANDialog(_ => Task.FromResult<string>(null));
            var pending = new HBCIDialogResult(
                new List<HBCIBankMessage> { new HBCIBankMessage("3955", "Sicherheitsfreigabe erfolgt über anderen Kanal") },
                "HIRMS:3:2:4+3955::Sicherheitsfreigabe erfolgt über anderen Kanal'");

            var watch = Stopwatch.StartNew();
            var processSca = (Task<HBCIDialogResult>) typeof(FinTsClient)
                .GetMethod("ProcessSCA", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(client, new object[] { pending, dialog, false, null });
            _ = Task.Delay(TimeSpan.FromSeconds(7)).ContinueWith(_ => dialog.IsCancelWaitForApproval = true);

            await processSca;
            watch.Stop();
            return (requests, watch.Elapsed);
        }
        finally
        {
            stop.Cancel();
            listener.Stop();
            await server;
        }
    }

    private static async Task AnswerAsync(NetworkStream stream)
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

        var answer = Convert.ToBase64String(Encoding.Latin1.GetBytes(
            "HNHBK:1:3+000000000100+300+DIALOG+2+DIALOG:2'HIRMG:2:2+3060::Teilweise liegen Warnungen/Hinweise vor.'"
            + "HIRMS:3:2:3+3956::Starke Kundenauthentifizierung noch ausstehend.'HNHBS:4:1+2'"));
        var response = "HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\n"
            + $"Content-Length: {answer.Length}\r\nConnection: close\r\n\r\n{answer}";
        var bytes = Encoding.ASCII.GetBytes(response);
        await stream.WriteAsync(bytes, 0, bytes.Length);
    }
}
