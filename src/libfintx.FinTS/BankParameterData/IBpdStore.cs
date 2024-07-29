#nullable enable
using System.Threading.Tasks;

namespace libfintx.FinTS.BankParameterData;

/// <summary>
/// A generic interface for storing bank parameter data (BPD).
///
/// This data is once retrieved during synchronization and gives information about
/// how to contact the bank, what methods are available and others.
///
/// See also https://www.hbci-zka.de/dokumente/spezifikation_deutsch/fintsv3/FinTS_3.0_Formals_2017-10-06_final_version.pdf chapter C.
/// </summary>
public interface IBpdStore
{
    /// <summary>
    /// Returns the stored version of the bank parameter data.
    /// </summary>
    /// <param name="bankCountry">
    /// Bank country number. For germany the number 280 is used even the ISO code number is 271.
    ///
    /// See also https://www.hbci-zka.de/dokumente/spezifikation_deutsch/fintsv4/FinTS_4.1_Messages_Finanzdatenformate_2014-01-20-FV.pdf.
    /// </param>
    /// <param name="bankCode">
    /// Bank code. In german Bankleitzahl (BLZ).
    /// </param>
    /// <returns>
    /// The BPD version number currently stored in the data store.
    /// </returns>
    public Task<int?> GetBPDVersion(int bankCountry, int bankCode);

    /// <summary>
    /// Retrieves the bank parameter data for a specific bank from the store and returns it.
    /// </summary>
    /// <param name="bankCountry">
    /// Bank country number. For germany the number 280 is used even the ISO code number is 271.
    ///
    /// See also https://www.hbci-zka.de/dokumente/spezifikation_deutsch/fintsv4/FinTS_4.1_Messages_Finanzdatenformate_2014-01-20-FV.pdf.
    /// </param>
    /// <param name="bankCode">
    /// Bank code. In german Bankleitzahl (BLZ).
    /// </param>
    /// <returns>
    /// The BPD data raw.
    /// </returns>
    public Task<string?> GetBPD(int bankCountry, int bankCode);

    /// <summary>
    /// Saves bank parameter data for a specific bank. In case the BPD data is already present, it will be overwritten.
    /// </summary>
    /// <param name="bankCountry">
    /// Bank country number. For germany the number 280 is used even the ISO code number is 271.
    ///
    /// See also https://www.hbci-zka.de/dokumente/spezifikation_deutsch/fintsv4/FinTS_4.1_Messages_Finanzdatenformate_2014-01-20-FV.pdf.
    /// </param>
    /// <param name="bankCode">
    /// Bank code. In german Bankleitzahl (BLZ).
    /// </param>
    /// <param name="bpd">
    /// The BPD data raw.
    /// </param>
    /// <returns/>
    public Task SaveBPD(int bankCountry, int bankCode, string bpd);

    /// <summary>
    /// Deletes bank parameter data for a specific bank.
    /// </summary>
    /// <param name="bankCountry">
    /// Bank country number. For germany the number 280 is used even the ISO code number is 271.
    ///
    /// See also https://www.hbci-zka.de/dokumente/spezifikation_deutsch/fintsv4/FinTS_4.1_Messages_Finanzdatenformate_2014-01-20-FV.pdf.
    /// </param>
    /// <param name="bankCode">
    /// Bank code. In german Bankleitzahl (BLZ).
    /// </param>
    /// <returns/>
    public Task DeleteBPD(int bankCountry, int bankCode);
}
