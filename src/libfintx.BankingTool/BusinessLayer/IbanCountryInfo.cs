using System;
using System.Text.RegularExpressions;

namespace libfintx.BankingTool.BusinessLayer;

/// <summary>
/// A class holding information about a IBAN country definition.
/// </summary>
public class IbanCountryInfo
{
    public IbanCountryInfo()
    {
    }

    public IbanCountryInfo(string countryCode)
    {
        if (countryCode == null)
            throw new ArgumentNullException(nameof(countryCode));

        if (!IbanCountryData.TryFillDefaults(countryCode, this))
            throw new NotSupportedException($"No default data is registered for country code {countryCode}");
    }

    public string? CountryCode { get; set; }

    /// <summary>
    /// A optional regular expression a IBAN has to match. Including country code.
    /// </summary>
    public Regex? RegularExpression { get; set; }

    public int BankIdentLength { get; set; }

    public int AccountNumberLength { get; set; }

    public static IbanCountryInfo GetDefault(string countryCode)
    {
        return new IbanCountryInfo(countryCode);
    }
}
