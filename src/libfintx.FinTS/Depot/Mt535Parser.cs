/*	
 * 	
 *  This file is part of libfintx.
 *  
 *  Copyright (C) 2016 - 2022 Torsten Klinger
 * 	E-Mail: torsten.klinger@googlemail.com
 *  
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 3 of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software Foundation,
 *  Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 * 	
 */


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace libfintx.FinTS.Depot
{
    /// <summary>One position of a securities statement (MT535), as far as the bank reports it.</summary>
    public sealed class SecurityHolding
    {
        public string Isin { get; set; }
        public string Name { get; set; }

        /// <summary>Shares, or for bonds the nominal amount (see <see cref="IsNominal"/>).</summary>
        public decimal Quantity { get; set; }

        /// <summary>True when <see cref="Quantity"/> is a nominal amount (FAMT) rather than a number of shares (UNIT).</summary>
        public bool IsNominal { get; set; }

        /// <summary>The price, or null when the bank sends none.</summary>
        public decimal? Price { get; set; }

        /// <summary>The currency of <see cref="Price"/>; empty for a price in percent.</summary>
        public string PriceCurrency { get; set; } = string.Empty;

        /// <summary>True when the price is a percentage of the nominal amount, as for bonds.</summary>
        public bool PriceIsPercent { get; set; }

        /// <summary>The day of the price, or null when the bank sends none.</summary>
        public DateTime? PriceDate { get; set; }

        /// <summary>The value of the position, or null when the bank sends none.</summary>
        public decimal? Value { get; set; }

        /// <summary>The currency of <see cref="Value"/>.</summary>
        public string ValueCurrency { get; set; } = string.Empty;

        /// <summary>The acquisition price per share the bank reports, if any. Informational only.</summary>
        public decimal? AcquisitionPrice { get; set; }
    }

    /// <summary>A securities statement: the day it is for and its positions.</summary>
    public sealed class SecuritiesStatement
    {
        public DateTime? StatementDate { get; set; }
        public decimal? TotalValue { get; set; }
        public string TotalValueCurrency { get; set; } = string.Empty;
        public List<SecurityHolding> Holdings { get; } = new List<SecurityHolding>();
    }

    /// <summary>
    /// Reads a statement of holdings in SWIFT MT535 format as FinTS returns it in HIWPD. Only
    /// the fields a holdings overview needs are read: per financial instrument (between
    /// :16R:FIN and :16S:FIN) the ISIN and name (:35B:), the quantity (:93B::AGGR), the price
    /// (:90B::MRKT or :90A::MRKT for percent), its date (:98A::PRIC or :98C::PRIC), the value
    /// (:19A::HOLD) and the acquisition price (:70E::HOLD). Fields a bank leaves out stay empty
    /// rather than failing the statement.
    /// </summary>
    public static class Mt535Parser
    {
        private static readonly Regex Isin = new Regex(@"^ISIN\s+([A-Z]{2}[A-Z0-9]{9}[0-9])", RegexOptions.Compiled);
        private static readonly Regex Quantity = new Regex(@"^:93B::AGGR//(UNIT|FAMT)/(N?)([0-9,]+)", RegexOptions.Compiled);
        private static readonly Regex PriceAmount = new Regex(@"^:90B::MRKT//[A-Z]{4}/([A-Z]{3})([0-9,]+)", RegexOptions.Compiled);
        private static readonly Regex PricePercent = new Regex(@"^:90A::MRKT//[A-Z]{4}/(N?)([0-9,]+)", RegexOptions.Compiled);
        private static readonly Regex PriceDate = new Regex(@"^:98[AC]::PRIC//([0-9]{8})", RegexOptions.Compiled);
        private static readonly Regex HoldValue = new Regex(@"^:19A::HOLD//(N?)([A-Z]{3})([0-9,]+)", RegexOptions.Compiled);
        private static readonly Regex Acquisition = new Regex(@"^:70E::HOLD//[^|]*\|2([0-9]*,[0-9]*)\+([A-Z]{3})", RegexOptions.Compiled);
        private static readonly Regex StatementDate = new Regex(@"^:98[AC]::STAT//([0-9]{8})", RegexOptions.Compiled);

        public static SecuritiesStatement Parse(string mt535)
        {
            var statement = new SecuritiesStatement();
            SecurityHolding current = null;
            foreach (string field in Fields(mt535 ?? string.Empty))
            {
                if (field.StartsWith(":16R:FIN", StringComparison.Ordinal) && !field.StartsWith(":16R:FINA", StringComparison.Ordinal))
                {
                    current = new SecurityHolding();
                    continue;
                }
                if (field.StartsWith(":16S:FIN", StringComparison.Ordinal) && !field.StartsWith(":16S:FINA", StringComparison.Ordinal))
                {
                    if (current != null)
                        statement.Holdings.Add(current);
                    current = null;
                    continue;
                }

                Match m;
                if (current == null)
                {
                    if ((m = StatementDate.Match(field)).Success)
                        statement.StatementDate = Date(m.Groups[1].Value);
                    else if ((m = HoldValue.Match(field)).Success)
                    {
                        statement.TotalValue = Amount(m.Groups[3].Value, m.Groups[1].Value == "N");
                        statement.TotalValueCurrency = m.Groups[2].Value;
                    }
                    continue;
                }

                if (field.StartsWith(":35B:", StringComparison.Ordinal))
                {
                    // ISIN on the first line, then an optional local code line (/DE/123456), then the name.
                    string[] lines = field.Substring(5).Split('|');
                    if ((m = Isin.Match(lines[0])).Success)
                        current.Isin = m.Groups[1].Value;
                    var nameLines = new List<string>();
                    for (int i = 1; i < lines.Length; i++)
                        if (!lines[i].StartsWith("/", StringComparison.Ordinal))
                            nameLines.Add(lines[i].Trim());
                    current.Name = string.Join(" ", nameLines);
                }
                else if ((m = Quantity.Match(field)).Success)
                {
                    current.IsNominal = m.Groups[1].Value == "FAMT";
                    current.Quantity = Amount(m.Groups[3].Value, m.Groups[2].Value == "N");
                }
                else if ((m = PriceAmount.Match(field)).Success)
                {
                    current.PriceCurrency = m.Groups[1].Value;
                    current.Price = Amount(m.Groups[2].Value, false);
                    current.PriceIsPercent = false;
                }
                else if ((m = PricePercent.Match(field)).Success)
                {
                    current.Price = Amount(m.Groups[2].Value, m.Groups[1].Value == "N");
                    current.PriceIsPercent = true;
                }
                else if ((m = PriceDate.Match(field)).Success)
                    current.PriceDate = Date(m.Groups[1].Value);
                else if ((m = HoldValue.Match(field)).Success)
                {
                    current.Value = Amount(m.Groups[3].Value, m.Groups[1].Value == "N");
                    current.ValueCurrency = m.Groups[2].Value;
                }
                else if ((m = Acquisition.Match(field)).Success)
                    current.AcquisitionPrice = Amount(m.Groups[1].Value, false);
            }
            return statement;
        }

        /// <summary>
        /// The fields of the message, each starting with ":"; continuation lines are joined to their
        /// field with "|". A line "-" ends a message.
        /// </summary>
        private static IEnumerable<string> Fields(string text)
        {
            string current = null;
            foreach (string raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = raw.TrimEnd();
                if (line.Length == 0)
                    continue;
                if (line.StartsWith(":", StringComparison.Ordinal))
                {
                    if (current != null)
                        yield return current;
                    current = line;
                }
                else if (line.StartsWith("-", StringComparison.Ordinal))
                {
                    if (current != null)
                        yield return current;
                    current = null;
                }
                else if (current != null)
                    current += "|" + line;
            }
            if (current != null)
                yield return current;
        }

        private static decimal Amount(string swift, bool negative)
        {
            // SWIFT amounts use a comma as decimal mark and may end with it ("100,").
            string normalized = swift.Replace(',', '.').TrimEnd('.');
            decimal value = decimal.Parse(normalized.Length == 0 ? "0" : normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
            return negative ? -value : value;
        }

        private static DateTime? Date(string yyyymmdd) =>
            DateTime.TryParseExact(yyyymmdd, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date) ? date : (DateTime?)null;
    }
}
