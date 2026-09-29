using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using libfintx.FinTS.Statement;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// Runs parser checks in globalization-invariant mode (e.g. containers without ICU), where only the invariant
/// culture exists and CultureInfo.GetCultureInfo("de-DE") throws CultureNotFoundException.
/// The mode is fixed at process start and cannot be switched on in-process, and changing CurrentCulture does not
/// reproduce it. So the parent test starts a child test host on this assembly with
/// DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 that runs only the Child_* test below.
/// </summary>
public class Test_InvariantGlobalizationMode
{
    private const string ChildMarker = "LIBFINTX_INVARIANT_GLOBALIZATION_CHILD";

    private static bool IsChild => Environment.GetEnvironmentVariable(ChildMarker) == "1";

    [Fact]
    public void MT942_Sums_Parse_In_Invariant_Globalization_Mode()
    {
        if (IsChild)
            return;

        var psi = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("test");
        psi.ArgumentList.Add(typeof(Test_InvariantGlobalizationMode).Assembly.Location);
        psi.ArgumentList.Add("--filter");
        psi.ArgumentList.Add($"FullyQualifiedName={typeof(Test_InvariantGlobalizationMode).FullName}.{nameof(Child_MT942_Sums)}");
        psi.Environment["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
        psi.Environment[ChildMarker] = "1";

        using var process = Process.Start(psi);
        // Read both streams asynchronously so the timeout below can actually fire.
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(300_000))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            Assert.True(false, $"Child test host timed out after 300 s:\n{stdoutTask.Result}\n{stderrTask.Result}");
        }
        var stdout = stdoutTask.Result;
        var stderr = stderrTask.Result;

        Assert.True(process.ExitCode == 0, $"Child test host failed (exit {process.ExitCode}):\n{stdout}\n{stderr}");
        // Guard against the filter silently matching nothing: exactly one test must have run and passed.
        Assert.True(Regex.IsMatch(stdout, @"Passed:\s+1\b") && Regex.IsMatch(stdout, @"Total:\s+1\b"),
            $"Expected exactly one child test to run and pass:\n{stdout}\n{stderr}");
    }

    [Fact]
    public void Child_MT942_Sums()
    {
        if (!IsChild)
            return; // Only meaningful inside the invariant-mode child started above.

        // Proves the child really runs in invariant mode.
        Assert.Throws<CultureNotFoundException>(() => CultureInfo.GetCultureInfo("de-DE"));

        const string mt942 = @"
:20:STARTDISPE
:25:10050000/0123456789
:28C:00000/001
:34F:EURD1826,90
:13:1911071300
:61:1911081105DR1826,90NDDTNONREF
:86:105?00FOLGELASTSCHRIFT?109248?20EREF+Zahlbeleg
:90D:1EUR1826,90
:90C:2EUR250,10
-
";
        var stmt = MT940.Deserialize(mt942, "0123456789", true).Single();

        Assert.Equal(1, stmt.CountDebit);
        Assert.Equal(-1826.90m, stmt.AmountDebit);
        Assert.Equal(2, stmt.CountCredit);
        Assert.Equal(250.10m, stmt.AmountCredit);
    }
}
