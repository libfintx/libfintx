#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace libfintx.FinTS.UserParameterData;

/// <summary>
/// A user parameter data store keeping the data in memory, so it never reaches the disk.
///
/// Note: The data will be lost after the object is deallocated.
/// </summary>
public class UpdInMemoryStore : IUpdStore
{
    private readonly Dictionary<Tuple<int, int, string>, string> _cache = new();

    public Task<string?> GetUPD(int bankCountry, int bankCode, string userId)
    {
        return Task.FromResult(_cache.TryGetValue(Tuple.Create(bankCountry, bankCode, userId), out var upd) ? upd : null);
    }

    public Task SaveUPD(int bankCountry, int bankCode, string userId, string upd)
    {
        _cache[Tuple.Create(bankCountry, bankCode, userId)] = upd;
        return Task.CompletedTask;
    }
}
