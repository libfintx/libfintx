#nullable enable
using System.Collections.Generic;

namespace libfintx.FinTS;

public static class BankReturnCodes
{
    private static readonly Dictionary<int, string> ReturnCodeMeaning = new()
    {
        // This list is taken from the documentation:
        //   FinTS_Rueckmeldungscodes_2021-07-07_final_version.pdf
        //
        // Please update when a newer version is available.
        // Note that when multiple meanings exist, they are not added here.

        // Success / "Erfolgsmeldungen"
        // Note: leading zeros are removed from the return code
        { 10, "Entgegengenommen" },
        { 31, "Auftragsstorno durchgeführt" },
        { 40, "Letzter Dialog endete am %1 um %2"},
        { 41, "Falls Datum/Uhrzeit nicht korrekt, wenden Sie sich an Ihren Berater unter %1 bzw. %2 "},
        { 90, "TAN OK (%1)"},
        { 100, "Beendet"},
        { 901, "PIN gültig"},
        //0950-0999 "Individuell"

        // Notes / "Hinweise"
        { 1010, "Es liegen neue Kontoinformationen vor " },
        { 1040, "BPD nicht mehr aktuell, aktuelle Version enthalten." },
        { 1050, "UPD nicht mehr aktuell, aktuelle Version enthalten. " },
        { 1060, "Teilweise liegen Hinweise vor" },
        //1950-1999: "Individuell"

        // Warnings
        // TBD

        // Errors
        // TBD
    };

    public static string? GetReturnCodeMeaning(int returnCode)
    {
        return ReturnCodeMeaning.TryGetValue(returnCode, out var returnCodeMeaning) ? returnCodeMeaning : null;
    }
}
