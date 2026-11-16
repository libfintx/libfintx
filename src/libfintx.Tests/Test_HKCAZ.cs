using System.Collections.Generic;
using libfintx.FinTS;
using libfintx.FinTS.Camt;
using libfintx.FinTS.Data;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// The camt statement request names the account the way HKKAZ does: as the bank lists it in the
/// UPD, even when the connection details carry only the account number.
/// </summary>
[Collection("UPD")]
public class Test_HKCAZ
{
    private static FinTsClient Client()
    {
        var client = TestHelper.CreateTestClient(new ConnectionDetails { Account = "15910698", Blz = 12030000 });
        client.HICAZS_Camt = CamtScheme.Camt052_001_08;
        return client;
    }

    [Fact]
    public void TheRequest_NamesTheAccountFromTheUpd()
    {
        var previous = UPD.AccountList;
        UPD.AccountList = new List<AccountInformation>
        {
            new AccountInformation
            {
                AccountIban = "DE02120300000015910698", AccountBic = "BYLADEM1001", AccountNumber = "15910698",
                AccountBankCode = "12030000", AccountType = "Girokonto", AccountPermissions = new List<AccountPermission>(),
            },
        };
        try
        {
            Assert.Equal(
                "HKCAZ:3:1+DE02120300000015910698:BYLADEM1001:15910698::280:12030000+urn?:iso?:std?:iso?:20022?:tech?:xsd?:camt.052.001.08+N'",
                HKCAZ.Segment(Client(), null, null, null, CamtVersion.Camt052));
        }
        finally
        {
            UPD.AccountList = previous;
        }
    }

    [Fact]
    public void ThePeriodAndTheTouchdownPoint_FollowTheFormat()
    {
        var previous = UPD.AccountList;
        UPD.AccountList = new List<AccountInformation>();
        try
        {
            Assert.Equal(
                "HKCAZ:3:1+::15910698::280:12030000+urn?:iso?:std?:iso?:20022?:tech?:xsd?:camt.052.001.08+N+20260101+20261116'",
                HKCAZ.Segment(Client(), "20260101", "20261116", null, CamtVersion.Camt052));
            Assert.Equal(
                "HKCAZ:3:1+::15910698::280:12030000+urn?:iso?:std?:iso?:20022?:tech?:xsd?:camt.052.001.08+N++++ABC'",
                HKCAZ.Segment(Client(), null, null, "ABC", CamtVersion.Camt052));
        }
        finally
        {
            UPD.AccountList = previous;
        }
    }
}
