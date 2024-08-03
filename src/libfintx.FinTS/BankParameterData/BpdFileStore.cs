#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libfintx.FinTS.Data.Segment;
using libfintx.FinTS.Segments;

namespace libfintx.FinTS.BankParameterData;

/// <summary>
/// A bank parameter data store saving the data on a local directory.
///
/// As it is defined in the FinTS specification, it uses the bank code as file name and .bpd as file extension.
/// </summary>
public class BpdFileStore : IBpdStore
{
    private readonly string _path;

    /// <summary>
    /// Creates a new bank parameter file store.
    /// </summary>
    /// <param name="path">
    /// The directory where to store the bpd files.
    /// </param>
    public BpdFileStore(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        _path = path;
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

        var segmentRaw = Helper.SplitSegments(bpd).First();

        var segment = (HIBPA)SegmentParserFactory.ParseSegment(segmentRaw);

        return segment.BpdVersion;
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
