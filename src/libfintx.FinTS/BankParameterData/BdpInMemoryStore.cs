#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using libfintx.FinTS.Data.Segment;
using libfintx.FinTS.Segments;

namespace libfintx.FinTS.BankParameterData;

/// <summary>
/// A bank parameter data store saving the data in memory.
///
/// Note: The data will be lost after the object is deallocated.
/// </summary>
public class BdpInMemoryStore : IBpdStore
{
    private readonly Dictionary<Tuple<int, int>, string> _cache = new();

    public async Task<int?> GetBPDVersion(int bankCountry,int bankCode)
    {
        var bpd = await GetBPD(bankCountry, bankCode);

        if (bpd == null)
        {
            return null;
        }

        var segmentRaw = Helper.SplitSegments(bpd).First();

        var segment = (HIBPA) SegmentParserFactory.ParseSegment(segmentRaw);

        return segment.BpdVersion;
    }

    public Task<string?> GetBPD(int bankCountry, int bankCode)
    {
        if (_cache.TryGetValue(new Tuple<int, int>(bankCountry, bankCode), out var bpd))
        {
            return Task.FromResult<string?>(bpd);
        }

        return Task.FromResult<string?>(null);
    }

    public Task SaveBPD(int bankCountry, int bankCode, string bpd)
    {
        _cache[new Tuple<int, int>(bankCountry, bankCode)] = bpd;

        return Task.CompletedTask;
    }

    public Task DeleteBPD(int bankCountry, int bankCode)
    {
        if (_cache.ContainsKey(new Tuple<int, int>(bankCountry, bankCode)))
        {
            _cache.Remove(new Tuple<int, int>(bankCountry, bankCode));
        }

        return Task.CompletedTask;
    }
}
