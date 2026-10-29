#nullable enable
using System.Threading.Tasks;

namespace libfintx.FinTS.UserParameterData;

/// <summary>
/// A generic interface for storing user parameter data (UPD).
///
/// The bank sends this data during dialog initialization. It lists the accounts of the user,
/// with account numbers, IBANs, account holders and the business transactions permitted for them.
/// Choose a store that fits how sensitive this data is to your application.
///
/// See also https://www.hbci-zka.de/dokumente/spezifikation_deutsch/fintsv3/FinTS_3.0_Formals_2017-10-06_final_version.pdf chapter C.
/// </summary>
public interface IUpdStore
{
    /// <summary>
    /// Retrieves the user parameter data of a user at a specific bank.
    /// </summary>
    /// <param name="bankCountry">
    /// Bank country number. For germany the number 280 is used even the ISO code number is 271.
    /// </param>
    /// <param name="bankCode">
    /// Bank code. In german Bankleitzahl (BLZ).
    /// </param>
    /// <param name="userId">
    /// The user ID the bank knows the user by.
    /// </param>
    /// <returns>
    /// The UPD data raw, or null when none is stored.
    /// </returns>
    public Task<string?> GetUPD(int bankCountry, int bankCode, string userId);

    /// <summary>
    /// Saves the user parameter data of a user at a specific bank, replacing what is stored.
    /// </summary>
    /// <param name="bankCountry">
    /// Bank country number. For germany the number 280 is used even the ISO code number is 271.
    /// </param>
    /// <param name="bankCode">
    /// Bank code. In german Bankleitzahl (BLZ).
    /// </param>
    /// <param name="userId">
    /// The user ID the bank knows the user by.
    /// </param>
    /// <param name="upd">
    /// The UPD data raw.
    /// </param>
    /// <returns/>
    public Task SaveUPD(int bankCountry, int bankCode, string userId, string upd);
}
