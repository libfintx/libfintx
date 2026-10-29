using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using libfintx.FinTS;
using libfintx.FinTS.BankParameterData;
using libfintx.FinTS.Data;
using libfintx.FinTS.UserParameterData;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// A client given a customer system ID skips the synchronization, so the bank sends the bank
/// parameter data only when the stored ones are outdated. The client then takes the segment
/// versions from the stored ones. The user parameter data go to the store the caller chooses.
/// </summary>
[Collection("UPD")]
public class Test_StoredParameterData
{
    private static readonly ConnectionDetails Connection = new()
    {
        Blz = 1234567,
        UserId = "1234567"
    };

    private static string BankMessage()
    {
        string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources/BankMessage_1.txt");
        return File.ReadAllText(path);
    }

    private static FinTsClient CreateClient(IBpdStore bpdStore, IUpdStore updStore) =>
        new(Connection, bpdDataStore: bpdStore, loggerFactory: NullLoggerFactory.Instance, updDataStore: updStore);

    [Fact]
    public void StoredBankParameters_GiveTheSegmentVersions()
    {
        var bpdStore = new BdpInMemoryStore();
        var received = CreateClient(bpdStore, new UpdInMemoryStore());
        received.Parse_Segments(BankMessage());

        var stored = CreateClient(bpdStore, new UpdInMemoryStore());
        stored.HIRMS = received.HIRMS;
        stored.ApplyStoredBankParameters();

        Assert.Equal(received.HISALS, stored.HISALS);
        Assert.Equal(received.HIKAZS, stored.HIKAZS);
        Assert.Equal(received.HISPAS, stored.HISPAS);
        Assert.Equal(received.HITANS, stored.HITANS);
        Assert.NotEqual(0, stored.HITANS);
    }

    [Fact]
    public void StoredBankParameters_WithoutTanMethod_LeaveHktanOpen()
    {
        var bpdStore = new BdpInMemoryStore();
        CreateClient(bpdStore, new UpdInMemoryStore()).Parse_Segments(BankMessage());

        var stored = CreateClient(bpdStore, new UpdInMemoryStore());
        stored.ApplyStoredBankParameters();

        Assert.Equal(0, stored.HITANS);
        Assert.Equal(7, stored.HISALS);
    }

    [Fact]
    public void NoStoredBankParameters_ChangeNothing()
    {
        var client = CreateClient(new BdpInMemoryStore(), new UpdInMemoryStore());
        client.ApplyStoredBankParameters();

        Assert.Equal(0, client.HISALS);
        Assert.Equal(0, client.HITANS);
    }

    [Fact]
    public async Task UserParameters_GoToTheGivenStore()
    {
        var updStore = new UpdInMemoryStore();
        CreateClient(new BdpInMemoryStore(), updStore).Parse_Segments(BankMessage());

        var upd = await updStore.GetUPD(280, Connection.Blz, Connection.UserId);
        Assert.StartsWith("HIUPA", upd);
    }
}
