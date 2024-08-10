using libfintx.FinTS;
using libfintx.FinTS.BankParameterData;
using libfintx.FinTS.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace libfintx.Tests;

internal static class TestHelper
{
    public static FinTsClient CreateTestClient(ConnectionDetails? connectionDetails = null)
    {
        return new FinTsClient(connectionDetails ?? new ConnectionDetails(),
            bpdDataStore: new BdpInMemoryStore(),
            loggerFactory: NullLoggerFactory.Instance);
    }
}
