#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libfintx.FinTS.Data.Segment;
using libfintx.FinTS.Segments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace libfintx.FinTS.BankParameterData;

/// <summary>
/// A bank parameter data store saving the data on a local directory.
///
/// As it is defined in the FinTS specification, it uses the bank code as file name and .bpd as file extension.
/// </summary>
public class BpdFileStore : IBpdStore
{
    private readonly string _path;
    private readonly ILogger<BpdFileStore> _logger;

    /// <summary>
    /// Creates a new bank parameter file store.
    /// </summary>
    /// <param name="path">
    /// The directory where to store the bpd files.
    /// </param>
    /// <param name="loggerFactory">
    /// A logger factory used to report cache anomalies (e.g. a discarded malformed BPD file).
    /// When not given, no output is logged.
    /// </param>
    public BpdFileStore(string path, ILoggerFactory? loggerFactory = null)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        _path = path;
        _logger = loggerFactory?.CreateLogger<BpdFileStore>()
                  ?? NullLoggerFactory.Instance.CreateLogger<BpdFileStore>();
    }

    private string BuildFilePath(int bankCountry, int bankCode)
    {
        var file = Path.Combine(_path, $"{bankCode}.bpd");

        // Before the files where named '280_*.bpd'. But this is not according to the FinTS standard.
        // To fix this, the following workaround will rename the files.
        // 
        // START/ This code section could be deleted when not needing downward compatibility.
        var oldFile = Path.Combine(_path, $"{bankCountry}_{bankCode}.bpd");
        if (File.Exists(oldFile) && !File.Exists(file))
        {
            File.Move(oldFile, file);
        }
        // END/ This code section could be deleted when not needing downward compatibility.

        return file;
    }

    public async Task<int?> GetBPDVersion(int bankCountry, int bankCode)
    {
        var bpd = await GetBPD(bankCountry, bankCode);

        if (bpd == null)
        {
            return null;
        }

        try
        {
            var segmentRaw = Helper.SplitSegments(bpd).FirstOrDefault();
            if (segmentRaw == null)
                return null;

            return (SegmentParserFactory.ParseSegment(segmentRaw) as HIBPA)?.BpdVersion;
        }
        catch (ArgumentException ex)
        {
            // Cached BPD file is malformed (e.g. truncated, partial PAIN payload). Drop it so the
            // caller re-fetches a fresh BPD from the bank instead of crashing every sync.
            // Logged so a recurring delete/re-fetch loop (bank keeps returning unparseable BPD)
            // is visible instead of silently retried.
            _logger.LogWarning("Discarding malformed cached BPD for bank {BankCode}; re-fetching from bank. {Message}", bankCode, ex.Message);
            await DeleteBPD(bankCountry, bankCode);
            return null;
        }
    }

    public Task<string?> GetBPD(int bankCountry, int bankCode)
    {
        return Task.Run(() =>
        {
            var file = BuildFilePath(bankCountry, bankCode);

            if (!File.Exists(file))
            {
                return null;
            }

            return File.ReadAllText(file, Encoding.UTF8);
        });
    }

    public Task SaveBPD(int bankCountry, int bankCode, string bpd)
    {
        return Task.Run(() =>
        {
            var file = BuildFilePath(bankCountry, bankCode);

            File.WriteAllText(file, bpd, Encoding.UTF8);
        });
    }

    public Task DeleteBPD(int bankCountry, int bankCode)
    {
        return Task.Run(() =>
        {
            var file = BuildFilePath(bankCountry, bankCode);

            if (File.Exists(file))
            {
                File.Delete(file);
            }
        });
    }
}
