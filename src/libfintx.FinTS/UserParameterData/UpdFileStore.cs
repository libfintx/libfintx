#nullable enable
using System.IO;
using System.Threading.Tasks;

namespace libfintx.FinTS.UserParameterData;

/// <summary>
/// A user parameter data store saving the data as plain text files, one per bank and user.
/// </summary>
public class UpdFileStore : IUpdStore
{
    private readonly string _path;

    public UpdFileStore(string path)
    {
        _path = path;
    }

    private string BuildFilePath(int bankCountry, int bankCode, string userId)
    {
        return Path.Combine(_path, $"{bankCountry}_{bankCode}_{Helper.MakeFilenameValid(userId)}.upd");
    }

    public Task<string?> GetUPD(int bankCountry, int bankCode, string userId)
    {
        return Task.Run(() =>
        {
            var file = BuildFilePath(bankCountry, bankCode, userId);

            return File.Exists(file) ? File.ReadAllText(file) : null;
        });
    }

    public Task SaveUPD(int bankCountry, int bankCode, string userId, string upd)
    {
        return Task.Run(() =>
        {
            Directory.CreateDirectory(_path);
            File.WriteAllText(BuildFilePath(bankCountry, bankCode, userId), upd);
        });
    }
}
