/*
 *
 *	Based on Timotheus Pokorra's C# implementation of OpenPetraPlugin_BankimportMT940,
 *	available at https://github.com/SolidCharity/OpenPetraPlugin_BankimportMT940/blob/master/Client/ParseMT940.cs
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace libfintx.Swift;

/// <summary>
/// A MT940/MT942 statement parser.
/// </summary>
// ReSharper disable once InconsistentNaming
public class MT940Parser
{
    private readonly ILogger<MT940Parser> _logger;

    /// <summary>
    /// Initializes the parser class.
    /// </summary>
    /// <param name="pending">
    /// If the file is a pending file (MT942) or a final file (MT940).
    /// </param>
    /// <param name="loggerFactory">
    /// A logging factory to create the logging where to write warnings.
    /// </param>
    public MT940Parser(bool pending = false, ILoggerFactory? loggerFactory = null)
    {
        SetPending = pending;
        _logger = loggerFactory?.CreateLogger<MT940Parser>();
    }

    protected SwiftStatement PreviousSwiftStatement = null;
    protected SwiftStatement CurrentSwiftStatement = null;

    public string Account { get; } = null;
    public bool SetPending { get; } = false;

    private static string LTrim(string code)
    {
        // Cut off leading zeros
        try
        {
            return Convert.ToInt64(code).ToString();
        }
        catch (Exception)
        {
            // IBAN or BIC
            return code;
        }
    }

    private IEnumerable<SwiftStatement> Data(string swiftTag, string swiftData)
    {
        if (CurrentSwiftStatement != null)
        {
            CurrentSwiftStatement.Lines.Add(new SwiftLine(swiftTag, swiftData));
        }

        if (swiftTag == "OS")
        {
            // Ignore
        }
        else if (swiftTag == "20")
        {
            // 20 is used for each "page" of the SWIFTStatement; but we want to put all SWIFTTransactions together
            // the whole SWIFTStatement closes with 62F
            if (CurrentSwiftStatement == null)
            {
                CurrentSwiftStatement = new SwiftStatement() { Type = swiftData };
                CurrentSwiftStatement.Lines.Add(new SwiftLine(swiftTag, swiftData));
            }
        }
        else if (swiftTag == "25")
        {
            int posSlash = swiftData.IndexOf("/");
            if (posSlash >= 0)
            {
                Debug.Assert(CurrentSwiftStatement != null);
                CurrentSwiftStatement.BankCode = swiftData.Substring(0, posSlash);
                if (posSlash < swiftData.Length)
                    CurrentSwiftStatement.AccountCode = LTrim(swiftData.Substring(posSlash + 1));
            }
        }
        else if (swiftTag.StartsWith("60")) // Anfangssaldo
        {
            // 60M is the start balance on each page of the SWIFTStatement.
            // 60F is the start balance of the whole SWIFTStatement.

            // First character is D or C
            int DebitCreditIndicator = (swiftData[0] == 'D' ? -1 : +1);

            // Next 6 characters: YYMMDD
            swiftData = swiftData.Substring(1);

            // Start date YYMMDD
            DateTime postingDate = new DateTime(2000 + Convert.ToInt32(swiftData.Substring(0, 2)),
                Convert.ToInt32(swiftData.Substring(2, 2)),
                Convert.ToInt32(swiftData.Substring(4, 2)));

            // Next 3 characters: Currency
            // Last characters: Balance with comma for decimal point
            Debug.Assert(CurrentSwiftStatement != null);
            CurrentSwiftStatement.Currency = swiftData.Substring(6, 3);
            try
            {
                decimal balance = DebitCreditIndicator * Convert.ToDecimal(swiftData.Substring(9).Replace(",",
                    Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator));

                // Use first start balance. If missing, use intermediate balance.
                if (swiftTag == "60F" || CurrentSwiftStatement.StartBalance == 0 && swiftTag == "60M")
                {
                    CurrentSwiftStatement.StartBalance = balance;
                    CurrentSwiftStatement.EndBalance = balance;
                }
            }
            catch (FormatException)
            {
                _logger.LogWarning($"Invalid balance: {swiftData}");
            }

            if (swiftTag == "60F" || swiftTag == "60M")
            {
                CurrentSwiftStatement.StartDate = postingDate;
            }
        }
        else if (swiftTag == "28C")
        {
            // this contains the number of the SWIFTStatement and the number of the page
            // only use for first page
            Debug.Assert(CurrentSwiftStatement != null);
            if (CurrentSwiftStatement.SwiftTransactions.Count == 0)
            {
                if (swiftData.IndexOf("/") != -1)
                {
                    CurrentSwiftStatement.Id = swiftData.Substring(0, swiftData.IndexOf("/"));
                }
                else
                {
                    // Realtime SWIFTStatement.
                    // Not use SWIFTStatement number 0, because Sparkasse has 0/1 for valid SWIFTStatements
                    CurrentSwiftStatement.Id = string.Empty;
                }
            }
        }
        else if (swiftTag == "61")
        {
            // If there is no SWIFTStatement available, create one
            if (CurrentSwiftStatement == null)
            {
                CurrentSwiftStatement = new SwiftStatement();
                CurrentSwiftStatement.Lines.Add(new SwiftLine(swiftTag, swiftData));
            }

            var SWIFTTransaction = new SwiftTransaction();
            CurrentSwiftStatement.SwiftTransactions.Add(SWIFTTransaction);

            // Valuta date (YYMMDD)
            try
            {
                SWIFTTransaction.ValueDate = new DateTime(2000 + Convert.ToInt32(swiftData.Substring(0, 2)),
                    Convert.ToInt32(swiftData.Substring(2, 2)),
                    Convert.ToInt32(swiftData.Substring(4, 2)));
            }
            catch (ArgumentOutOfRangeException)
            {
                // we have had the situation in the bank file with a date 30 Feb 2010.
                // probably because the instruction by the donor is to transfer the money on the 30 day each month
                // use the last day of the month
                int year = 2000 + Convert.ToInt32(swiftData.Substring(0, 2));
                int month = Convert.ToInt32(swiftData.Substring(2, 2));
                int day = DateTime.DaysInMonth(year, month);

                SWIFTTransaction.ValueDate = new DateTime(year, month, day);
            }

            swiftData = swiftData.Substring(6);

            // Optional: Posting date (MMDD)
            if (Regex.IsMatch(swiftData, @"^\d{4}"))
            {
                int year = SWIFTTransaction.ValueDate.Year;
                int month = Convert.ToInt32(swiftData.Substring(0, 2));
                int day = Convert.ToInt32(swiftData.Substring(2, 2));

                // Posting date 30 Dec 2020, Valuta date 1 Jan 2020
                if (month > SWIFTTransaction.ValueDate.Month && month == SWIFTTransaction.ValueDate.AddMonths(-1).Month)
                {
                    year--;
                }
                // Posting date 1 Jan 2020, Valuta date 30 Dec 2020
                else if (month < SWIFTTransaction.ValueDate.Month && month == SWIFTTransaction.ValueDate.AddMonths(1).Month)
                {
                    year++;
                }

                SWIFTTransaction.InputDate = new DateTime(year, month, day);

                swiftData = swiftData.Length > 4 ? swiftData.Substring(4) : string.Empty;
            }

            // Amount - some characters followed by an 'N'
            if (Regex.IsMatch(swiftData, @"^.+N"))
            {
                // Debit or credit, or storno debit or credit
                int debitCreditIndicator;
                if (swiftData[0] == 'R')
                {
                    // Storno means: reverse the debit credit flag
                    debitCreditIndicator = (swiftData[1] == 'D' ? 1 : -1);
                    swiftData = swiftData.Substring(2);
                }
                else
                {
                    debitCreditIndicator = (swiftData[0] == 'D' ? -1 : 1);
                    swiftData = swiftData.Substring(1);
                }

                // Sometimes there is something about currency
                if (char.IsLetter(swiftData[0]))
                {
                    // Just skip it for the moment
                    swiftData = swiftData.Substring(1);
                }

                // The amount, finishing with N
                SWIFTTransaction.Amount =
                    debitCreditIndicator * Convert.ToDecimal(swiftData.Substring(0, swiftData.IndexOf("N")).Replace(",",
                        Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator));

                CurrentSwiftStatement.EndBalance += SWIFTTransaction.Amount;

                var constIdx = swiftData.IndexOf("N");
                swiftData = swiftData.Length > constIdx ? swiftData.Substring(constIdx) : string.Empty;
            }
            else
            {
                yield break;
            }

            // Buchungsschlüssel
            if (Regex.IsMatch(swiftData, @"^N[A-Z0-9]{3}"))
            {
                SWIFTTransaction.TransactionTypeId = swiftData.Substring(0, 4);

                swiftData = swiftData.Length > 4 ? swiftData.Substring(4) : string.Empty;
            }
            else
            {
                yield break;
            }

            // customer reference
            if (Regex.IsMatch(swiftData, @"^.+"))
            {
                int idxDelimiter = swiftData.IndexOf("//");
                if (idxDelimiter > 0)
                    SWIFTTransaction.CustomerReference = swiftData.Substring(0, idxDelimiter);
                else
                    SWIFTTransaction.CustomerReference = swiftData;

                if (idxDelimiter > 0)
                    swiftData = swiftData.Length > idxDelimiter + 2 ? swiftData.Substring(idxDelimiter + 2) : string.Empty;
                else
                    swiftData = string.Empty;
            }
            else
            {
                yield break;
            }

            // Optional: bank reference; ends with CR/LF if followed by other data
            if (Regex.IsMatch(swiftData, @"^.+?\r\n", RegexOptions.Singleline))
            {
                int lineBreakIdx = swiftData.IndexOf("\r\n");
                if (lineBreakIdx > 0)
                {
                    SWIFTTransaction.BankReference = swiftData.Substring(0, lineBreakIdx);
                    swiftData = swiftData.Substring(lineBreakIdx + 2);
                }
                else
                {
                    SWIFTTransaction.BankReference = swiftData;
                    swiftData = string.Empty;
                }
            }

            // Optional: other data
            if (!string.IsNullOrWhiteSpace(swiftData))
            {
                SWIFTTransaction.OtherInformation = swiftData;
            }
        }
        else if (swiftTag == "86")
        {
            // Remove line breaks
            swiftData = swiftData.Replace("\r\n", string.Empty);

            Debug.Assert(CurrentSwiftStatement != null);
            SwiftTransaction SWIFTTransaction = CurrentSwiftStatement.SwiftTransactions[CurrentSwiftStatement.SwiftTransactions.Count - 1];

            // Geschaeftsvorfallcode
            SWIFTTransaction.TypeCode = swiftData.Substring(0, 3);

            swiftData = swiftData.Substring(3);

            if (swiftData.Length == 0)
                yield break;

            char separator = swiftData[0];

            swiftData = swiftData.Substring(1);

            string[] elements = swiftData.Split(new char[] { separator });
            string lastDescriptionSubfield = string.Empty;
            foreach (string element in elements)
            {
                int key = 0;
                string value = element;

                try
                {
                    key = Convert.ToInt32(element.Substring(0, 2));
                    value = element.Substring(2);
                }
                catch
                {
                    // If there is a question mark in the description, then we get here
                }

                if (key == 0)
                {
                    // Buchungstext
                    SWIFTTransaction.Text = value;
                }
                else if (key == 10)
                {
                    // Primanotennummer
                    SWIFTTransaction.Primanota = value;
                }
                else if ((key >= 11) && (key <= 19))
                {
                    // Ignore
                    // Unknown meaning
                }
                else if ((key >= 20) && (key <= 29))
                {
                    // No space between description lines
                    if (!value.EndsWith(" ")) value += " ";
                    SWIFTTransaction.Description += value;
                }
                else if (key == 30)
                {
                    SWIFTTransaction.BankCode = value;
                }
                else if (key == 31)
                {
                    SWIFTTransaction.AccountCode = value;
                }
                else if ((key == 32) || (key == 33))
                {
                    SWIFTTransaction.PartnerName += value;
                }
                else if (key == 34)
                {
                    // Textschlüsselergänzung
                    SWIFTTransaction.TextKeyAddition = value;
                }
                else if ((key == 35) || (key == 36))
                {
                    // Empfängername
                    SWIFTTransaction.Description += value;
                }
                else if ((key >= 60) && (key <= 63))
                {
                    SWIFTTransaction.Description += value;
                }
                else
                {
                    // Unknown key
                    yield break;
                }
            }
        }
        else if (swiftTag.StartsWith("62")) // Schlusssaldo
        {
            // 62M: Finish page
            // 62F: Finish SWIFTStatement
            int debitCreditIndicator = (swiftData[0] == 'D' ? -1 : 1);
            swiftData = swiftData.Substring(1);

            // Posting date YYMMDD
            DateTime postingDate = new DateTime(2000 + Convert.ToInt32(swiftData.Substring(0, 2)),
                Convert.ToInt32(swiftData.Substring(2, 2)),
                Convert.ToInt32(swiftData.Substring(4, 2)));

            swiftData = swiftData.Substring(6);

            // Currency
            if (swiftData.Length > 3) // Assure that currency and end balance are valid
            {
                Debug.Assert(CurrentSwiftStatement != null);
                swiftData = swiftData.Substring(3);

                // Sometimes, this line is the last line, and it has -NULNULNUL at the end
                if (swiftData.Contains("-\0"))
                {
                    swiftData = swiftData.Substring(0, swiftData.IndexOf("-\0"));
                }

                // End balance
                decimal endBalance = debitCreditIndicator * Convert.ToDecimal(swiftData.Replace(",",
                    Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator));
                CurrentSwiftStatement.EndBalance = endBalance;
            }

            if (swiftTag == "62F" || swiftTag == "62M")
            {
                Debug.Assert(CurrentSwiftStatement != null);
                CurrentSwiftStatement.EndDate = postingDate;
                FinalizeStatement(CurrentSwiftStatement);
                yield return CurrentSwiftStatement;
                PreviousSwiftStatement = CurrentSwiftStatement;
                CurrentSwiftStatement = null;
            }
        }
        else if (swiftTag == "64")
        {
            // Valutensaldo
        }
        else if (swiftTag == "65")
        {
            // Future valutensaldo
        }

        // Begin MT942
        else if (swiftTag == "34F")
        {
            Debug.Assert(CurrentSwiftStatement != null);
            if (swiftData.Length >= 3)
            {
                CurrentSwiftStatement.Currency = swiftData.Substring(0, 3);
                swiftData = swiftData.Length > 3 ? swiftData.Substring(3) : string.Empty;
            }

            // Kleinster Betrag der gemeldeten Umsätze
            if (Regex.IsMatch(swiftData, @"D?\d+,\d*"))
            {
                bool debit = swiftData.Substring(0, 1) == "D";
                decimal amount = 0;
                if (debit)
                {
                    decimal.TryParse(swiftData.Substring(1), out amount);
                    amount = amount * -1;
                }
                else
                {
                    decimal.TryParse(swiftData, out amount);
                }

                CurrentSwiftStatement.SmallestAmount = amount;
            }
            // Kleinster Betrag der gemeldeten Haben-Umsätze
            else if (Regex.IsMatch(swiftData, @"C\d+,\d*"))
            {
                decimal.TryParse(swiftData.Substring(1), out decimal amount);

                CurrentSwiftStatement.SmallestCreditAmount = amount;
            }

        }
        else if (swiftTag == "13") // Deutsche Bank
        {
            if (Regex.IsMatch(swiftData, @"\d{10}"))
            {
                DateTime.TryParseExact(swiftData, "yyMMddHHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime creationDate);

                Debug.Assert(CurrentSwiftStatement != null);
                CurrentSwiftStatement.CreationDate = creationDate;
            }
        }
        else if (swiftTag == "13D")
        {
            Debug.Assert(CurrentSwiftStatement != null);
            if (Regex.IsMatch(swiftData, @"\d{10}(\+|-)\d{4}"))
            {
                // Easier parsing
                // 1912090901+0100 -> 1912090901+01:00
                var dateStr = swiftData.Substring(0, 13) + ":" + swiftData.Substring(13, 2);
                DateTimeOffset.TryParseExact(dateStr, "yyMMddHHmmzzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dateTimeOffset);

                CurrentSwiftStatement.CreationDate = dateTimeOffset.DateTime;
            }
            else
            {
                DateTime.TryParseExact(swiftData, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime creationDate);

                CurrentSwiftStatement.CreationDate = creationDate;
            }
        }
        else if (swiftTag == "90D" || swiftTag == "90C")
        {
            bool debit = swiftTag == "90D";
            bool previousTag90d = !debit && CurrentSwiftStatement == null; // Previous tag has been 90D

            if (previousTag90d)
                CurrentSwiftStatement = PreviousSwiftStatement;

            if (CurrentSwiftStatement == null)
                yield break;

            int count = 0;
            decimal amount = 0;
            string currency = null;
            var match = Regex.Match(swiftData, @"(\d+)([A-Z]{3})(\d+(,\d+)?)");
            if (match.Success)
            {
                int.TryParse(match.Groups[1].Value, out count);

                currency = match.Groups[2].Value;

                decimal.TryParse(match.Groups[3].Value, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("de-DE"), out amount);
            }

            if (CurrentSwiftStatement.Currency == null)
                CurrentSwiftStatement.Currency = currency;

            if (debit)
            {
                CurrentSwiftStatement.CountDebit = count;
                CurrentSwiftStatement.AmountDebit = amount * -1;
            }
            else
            {
                CurrentSwiftStatement.CountCredit = count;
                CurrentSwiftStatement.AmountCredit = amount;
            }

            if (debit)
            {
                FinalizeStatement(CurrentSwiftStatement);
                yield return CurrentSwiftStatement;
                PreviousSwiftStatement = CurrentSwiftStatement;
                CurrentSwiftStatement = null;
            }
            else
            {
                if (!previousTag90d)
                {
                    FinalizeStatement(CurrentSwiftStatement);
                    yield return CurrentSwiftStatement;
                    PreviousSwiftStatement = CurrentSwiftStatement;
                }
                CurrentSwiftStatement = null;
            }
        }
        // End MT942

        else
        {
            // Unknown tag
            yield break;
        }
    }

    private void FinalizeStatement(SwiftStatement statement)
    {
        // Process missing input dates
        foreach (var tx in statement.SwiftTransactions)
        {
            if (tx.InputDate == default)
            {
                tx.InputDate = statement.EndDate;
            }
        }

        // Set pending
        if (SetPending)
        {
            statement.Pending = true;
        }

        // Parse SEPA purposes
        foreach (var tx in statement.SwiftTransactions)
        {
            if (string.IsNullOrWhiteSpace(tx.Description))
                continue;
            tx.Description = tx.Description.TrimEnd();
            // Collect all occuring SEPA purposes ordered by their position
            var indices = new List<Tuple<int, SepaPurpose>>();
            foreach (SepaPurpose sepaPurpose in Enum.GetValues(typeof(SepaPurpose)))
            {
                string prefix = $"{sepaPurpose}+";
                var idx = tx.Description.IndexOf(prefix);
                if (idx >= 0)
                {
                    indices.Add(Tuple.Create(idx, sepaPurpose));
                }
            }
            indices = indices.OrderBy(v => v.Item1).ToList();

            // Then get the values
            for (int i = 0; i < indices.Count; i++)
            {
                var beginIdx = indices[i].Item1 + $"{indices[i].Item2}+".Length;
                var endIdx = i < indices.Count - 1 ? indices[i + 1].Item1 : tx.Description.Length;

                var value = tx.Description.Substring(beginIdx, endIdx - beginIdx);
                tx.SepaPurposes[indices[i].Item2] = value;
            }
        }
    }


    private string _readLineCache = null;
    private string ReadLine(StreamReader streamReader)
    {
        string line;
        if (_readLineCache != null)
        {
            line = _readLineCache;
            _readLineCache = null;
        }
        else
        {
            line = streamReader.ReadLine();
        }

        if (line == null)
            return string.Empty;

        var index = line.IndexOf('@');
        if (index >= 0)
        {
            var newLine = line.Substring(0, index);
            if (line.Length - 1 > index)
            {
                _readLineCache = line.Substring(index);
            }

            line = newLine;
        }

        line = line.Replace("™", "Ö");
        line = line.Replace("š", "Ü");
        line = line.Replace("Ž", "Ä");
        line = line.Replace("á", "ß");
        line = line.Replace("\\", "Ö");
        line = line.Replace("]", "Ü");
        line = line.Replace("[", "Ä");
        line = line.Replace("~", "ß");

        return line;
    }

    /// <summary>
    /// Deserializes a MT940 statement into <see cref="SwiftStatement"/> objects.
    /// </summary>
    /// <param name="stream">
    /// The MT940 statement as stream.
    /// </param>
    /// <returns></returns>
    public IEnumerable<SwiftStatement> Deserialize(Stream stream)
    {
        PreviousSwiftStatement = null;
        CurrentSwiftStatement = null;

        var swiftRegex = new Regex(@"^:[\w]+:", RegexOptions.Compiled);
        string swiftTag = "";
        string swiftData = "";
        using (var sr = new StreamReader(stream))
        {
            while (!sr.EndOfStream)
            {
                string line = ReadLine(sr);

                if (line.Trim() == "-") // end of block
                {
                    // Process previously read swift chunk
                    if (swiftTag.Length > 0)
                    {
                        foreach (var statement in Data(swiftTag, swiftData))
                        {
                            yield return statement;
                        }
                    }

                    swiftTag = string.Empty;
                    swiftData = string.Empty;
                    continue;
                }

                if (line.Length > 0)
                {
                    // A swift chunk starts with a swiftTag, which is between colons
                    if (swiftRegex.IsMatch(line))
                    {
                        // Process previously read swift chunk
                        if (swiftTag.Length > 0)
                        {
                            foreach (var statement in Data(swiftTag, swiftData))
                            {
                                yield return statement;
                            }
                        }

                        int posColon = line.IndexOf(":", 2);

                        swiftTag = line.Substring(1, posColon - 1);
                        swiftData = line.Substring(posColon + 1);
                    }
                    else
                    {
                        // The swift chunk is spread over several lines
                        swiftData = swiftData + "\r\n" + line;
                    }
                }
            }
        }

        if (swiftTag.Length > 0)
        {
            foreach (var statement in Data(swiftTag, swiftData))
            {
                yield return statement;
            }
        }

        // If there are remaining unprocessed statements - add them
        if (CurrentSwiftStatement != null)
        {
            FinalizeStatement(CurrentSwiftStatement);
            yield return CurrentSwiftStatement;
            CurrentSwiftStatement = null;
        }

        PreviousSwiftStatement = null;
    }
}
