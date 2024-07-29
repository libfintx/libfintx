using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace libfintx.BankingTool.BusinessLayer;

internal static class IbanCountryData
{
    private static readonly Dictionary<string, (int, int, string?)> Data = new()
    {
        { "AE", (3, 16, null) },
        { "AT", (5, 11, null) },
        { "AZ", (4, 20, null) },
        { "BH", (4, 14, null) },
        { "CH", (5, 12, null) },
        { "CZ", (4, 16, null) },
        { "DE", (8, 10, "DE\\d{20}") },
        { "DO", (4, 20, null) },
        { "GE", (2, 16, null) },
        { "GI", (4, 15, null) },
        { "GT", (4, 20, null) },
        { "HR", (7, 10, null) },
        { "KW", (4, 22, null) },
        { "KZ", (3, 13, null) },
        { "LB", (4, 20, null) },
        { "LI", (5, 12, null) },
        { "LT", (5, 11, null) },
        { "LU", (3, 13, null) },
        { "LV", (4, 13, null) },
        { "MD", (2, 18, null) },
        { "MN", (4, 12, null) },
        { "NL", (4, 10, null) },
        { "NO", (4, 7, null) },
        { "QA", (4, 21, null) },
        { "RO", (4, 16, null) },
        { "SA", (2, 18, null) },
        { "SD", (2, 12, null) },
        { "SV", (4, 20, null) },
        { "TR", (5, 16, null) },
        { "UA", (6, 19, null) },
        { "VA", (3, 15, null) },
        { "VG", (4, 16, null) },
        { "XK", (4, 12, null) },

        // NOTE not all official supported:
        // see https://de.wikipedia.org/wiki/Internationale_Bankkontonummer
        // see https://en.wikipedia.org/wiki/International_Bank_Account_Number
    };

    public static bool TryFillDefaults(string countryCode, IbanCountryInfo ibanCountry)
    {
        if (!Data.TryGetValue(countryCode, out var countryData))
            return false;

        ibanCountry.BankIdentLength = countryData.Item1;
        ibanCountry.AccountNumberLength = countryData.Item2;
        if (countryData.Item3 != null)
        {
            ibanCountry.RegularExpression =
                new Regex(countryData.Item3, RegexOptions.CultureInvariant | RegexOptions.Compiled);
        }

        return true;
    }
}
