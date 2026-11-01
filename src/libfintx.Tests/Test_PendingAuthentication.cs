using System;
using System.Collections.Concurrent;
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
/// A decoupled approval that is cancelled or still pending after the last permitted status request
/// is no success, and the statement download then sends no HKKAZ. Answers to HKKAZ that carry only
/// warnings (3060, 3010, 3040, 3076) remain valid. A small local server plays the bank.
/// </summary>
[Collection("UPD")]
public class Test_PendingAuthentication
{
    private const string Header = "HNHBK:1:3+000000000100+300+DIALOG+2+DIALOG:2'";
    private const string Pending3955 = Header + "HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:4+3955::Sicherheitsfreigabe erfolgt ueber anderen Kanal.'HITAN:4:7:4+4++REF-4711+Bitte freigeben'HNHBS:5:1+2'";
    private const string Pending3956 = Header + "HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:3+3956::Starke Kundenauthentifizierung noch ausstehend.'HNHBS:4:1+2'";
    private const string Done = Header + "HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:3+0020::Auftrag ausgefuehrt.'HNHBS:4:1+2'";
    private const string InitWithoutSca = Header + "HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:4+0020::Auftrag ausgefuehrt.+3920::Zugelassene Zwei-Schritt-Verfahren fuer den Benutzer.:922'HNHBS:4:1+2'";

    private const string Mt940 = "\r\n:20:DEUTDEFFXXXX\r\n:25:10070124/123456789\r\n:28C:00000/000\r\n:60F:C191204EUR89,45\r\n:61:191204D1,00NMSCNONREF\r\n:86:116?20EREF+NOTPROVIDED?30BELADEBEXXX?32Mustermann, Max\r\n:62F:C191204EUR88,45\r\n-";

    private static string Hikaz(int segment) => $"HIKAZ:{segment}:7:3+@{Encoding.Latin1.GetByteCount(Mt940)}@{Mt940}'";

    /// <summary>BPD with a decoupled TAN method 922 and its status request parameters from HITANS#7.</summary>
    private static string Bpd(int maxRequests, int firstWait, int nextWait) =>
        "HIBPA:5:3:3+12+280:12345678+Testbank+0+1+300+500'"
        + $"HITANS:175:7:4+1+1+1+N:N:0:922:2:pushTAN-dec:Decoupled::pushTAN 2.0:::Aufforderung:2048:J:2:N:0:0:N:N:00:2:N:2:{maxRequests}:{firstWait}:{nextWait}:J:J'";

    private static (TANDialog Dialog, List<bool> Ends) CreateTanDialog()
    {
        var ends = new List<bool>();
        var dialog = new TANDialog(_ => Task.FromResult<string>(null), success => { ends.Add(success); return Task.CompletedTask; });
        return (dialog, ends);
    }

    private static Task<HBCIDialogResult> ProcessSca(FinTsClient client, HBCIDialogResult result, TANDialog dialog) =>
        (Task<HBCIDialogResult>) typeof(FinTsClient).GetMethod("ProcessSCA", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(client, new object[] { result, dialog, false });

    private static HBCIDialogResult ApprovalRequested() =>
        new(new List<HBCIBankMessage> { new("0010", "Nachricht entgegengenommen."), new("3955", "Sicherheitsfreigabe erfolgt ueber anderen Kanal.") }, Pending3955);

    private static string Kind(string message) =>
        message.Contains("HKKAZ") ? "HKKAZ" : message.Contains("HKIDN") ? "INIT" : message.Contains("HKTAN") ? "S" : "?";

    [Fact]
    public async Task LastStatusRequest_StillPending_IsNoSuccess()
    {
        using var bank = new TestBank(Bpd(2, 1, 1));
        bank.Client.HITAN = "REF-4711";
        var (dialog, ends) = CreateTanDialog();

        var result = await ProcessSca(bank.Client, ApprovalRequested(), dialog);

        Assert.Equal(2, bank.Sent.Count);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsWaitingForApproval);
        Assert.Equal(new[] { false }, ends);
    }

    [Fact]
    public async Task CancelBeforeFirstStatusRequest_IsNoSuccess_AndEndsTheWaitAtOnce()
    {
        using var bank = new TestBank(Bpd(0, 60, 60));
        bank.Client.HITAN = "REF-4711";
        var (dialog, _) = CreateTanDialog();

        var task = ProcessSca(bank.Client, ApprovalRequested(), dialog);
        await Task.Delay(500);
        var watch = Stopwatch.StartNew();
        dialog.IsCancelWaitForApproval = true;
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(watch.ElapsedMilliseconds < 2000);
        Assert.Empty(bank.Sent);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsApprovalRequired);
    }

    [Fact]
    public async Task Transactions_ApprovalStillPending_SendNoHkkaz()
    {
        using var bank = new TestBank(Bpd(2, 1, 1));
        var (dialog, _) = CreateTanDialog();

        var result = await bank.Client.Transactions(dialog, new DateTime(2026, 10, 1), new DateTime(2026, 10, 29));

        Assert.Equal(new[] { "INIT", "S", "S" }, bank.Sent.Select(Kind));
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Transactions_CancelledInFirstWait_SendNoHkkaz()
    {
        using var bank = new TestBank(Bpd(0, 30, 30));
        var (dialog, _) = CreateTanDialog();

        var task = bank.Client.Transactions(dialog, new DateTime(2026, 10, 1), new DateTime(2026, 10, 29));
        await Task.Delay(1500);
        dialog.IsCancelWaitForApproval = true;
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(new[] { "INIT" }, bank.Sent.Select(Kind));
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData("success", 1)]
    [InlineData("no-statements", 0)]
    [InlineData("second-page-success", 2)]
    [InlineData("second-page-warnings", 2)]
    public async Task Transactions_WithoutSca_AcceptAnswersWithWarnings(string answers, int statements)
    {
        Func<int, string> hkkaz = page => answers switch
        {
            "success" => Header + "HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:3+0020::Auftrag ausgefuehrt.'" + Hikaz(4) + "HNHBS:5:1+2'",
            "no-statements" => Header + "HIRMG:2:2+3060::Teilweise liegen Warnungen/Hinweise vor.'HIRMS:3:2:3+3010::Keine Umsaetze im angegebenen Zeitraum vorhanden.'HNHBS:4:1+2'",
            _ when page == 1 => Header + "HIRMG:2:2+3060::Teilweise liegen Warnungen/Hinweise vor.'HIRMS:3:2:3+3040::Es liegen weitere Informationen vor.:P1'" + Hikaz(4) + "HNHBS:5:1+2'",
            "second-page-success" => Header + "HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:3+0020::Auftrag ausgefuehrt.'" + Hikaz(4) + "HNHBS:5:1+2'",
            _ => Header + "HIRMG:2:2+3060::Teilweise liegen Warnungen/Hinweise vor.'HIRMS:3:2:3+3076::Keine starke Authentifizierung erforderlich.'" + Hikaz(4) + "HNHBS:5:1+2'",
        };
        using var bank = new TestBank(Bpd(2, 1, 1), hkkaz, InitWithoutSca);
        var (dialog, _) = CreateTanDialog();

        var result = await bank.Client.Transactions(dialog, new DateTime(2026, 10, 1), new DateTime(2026, 10, 29));

        Assert.False(result.HasError);
        Assert.NotNull(result.Data);
        Assert.Equal(statements, result.Data.Count);
    }

    /// <summary>
    /// Answers every FinTS message over HTTP on a local port: the dialog initialization asks for a
    /// decoupled approval unless told otherwise, each status request reports it still pending.
    /// </summary>
    private sealed class TestBank : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _server;
        private int _hkkazCount;

        public FinTsClient Client { get; }
        public ConcurrentQueue<string> Sent { get; } = new();

        public TestBank(string bpd, Func<int, string> hkkaz = null, string init = null)
        {
            _listener.Start();
            _server = Task.Run(() => Serve(hkkaz, init));

            Client = TestHelper.CreateTestClient(new ConnectionDetails
            {
                Url = $"http://127.0.0.1:{((IPEndPoint) _listener.LocalEndpoint).Port}/",
                Blz = 12345678,
                UserId = "testuser",
                Pin = "testpin",
                CustomerSystemId = "0",
                Account = "1234567890",
                Iban = "DE02120300000000202051",
                Bic = "BYLADEM1001",
            });
            Client.BPD = BPD.Parse(bpd);
            Client.HIRMS = "922";
            Client.HITANS = 7;
        }

        private async Task Serve(Func<int, string> hkkaz, string init)
        {
            while (!_stop.IsCancellationRequested)
            {
                TcpClient connection;
                try { connection = await _listener.AcceptTcpClientAsync(_stop.Token); }
                catch { break; }

                using (connection)
                {
                    var stream = connection.GetStream();
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
                            length = int.Parse(text.Substring(0, end).Split("\r\n")
                                .First(l => l.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)).Split(':')[1].Trim());
                        }
                    }

                    var body = Encoding.Latin1.GetString(Convert.FromBase64String(Encoding.ASCII.GetString(buffer.ToArray(), headerEnd, length)));
                    Sent.Enqueue(body);
                    string answer = body.Contains("HKKAZ") ? (hkkaz == null ? Done : hkkaz(Interlocked.Increment(ref _hkkazCount)))
                        : body.Contains("HKIDN") ? (init ?? Pending3955)
                        : body.Contains("HKTAN") ? Pending3956
                        : Done;
                    var encoded = Convert.ToBase64String(Encoding.Latin1.GetBytes(answer));
                    var bytes = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {encoded.Length}\r\nConnection: close\r\n\r\n{encoded}");
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
        }

        public void Dispose()
        {
            _stop.Cancel();
            _listener.Stop();
            try { _server.Wait(2000); }
            catch { }
        }
    }
}
