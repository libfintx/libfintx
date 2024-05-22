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
using System.IO;
using System.Linq;
using System.Text;
using libfintx.Globals;
using libfintx.Logger.Trace;
using libfintx.Swift;

namespace libfintx.FinTS.Statement
{
    /// <summary>
    /// MT940 account statement parser
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class MT940
    {
        /// <summary>
        /// Serializes a MT940 statement into a <see cref="SwiftStatement"/> class object.
        /// </summary>
        /// <param name="STA">
        /// The raw text of the MT940 statement.
        /// </param>
        /// <param name="account">
        /// The account name. Only required when <paramref name="writeToFile"/> is <c>true</c>.
        /// </param>
        /// <param name="writeToFile">
        /// If the MT940 statements shall be written to the disk.
        /// </param>
        /// <param name="pending"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="writeToFile"/> is <c>true</c> and <paramref name="account"/> is <c>null</c>.
        /// </exception>
        [Obsolete("Please use MT940.Deserialize(...) instead. For parameter writeToFile, use MT940.WriteToFile(...).")]
        public static List<SwiftStatement> Serialize(string STA, string account = null, bool writeToFile = false, bool pending = false)
        {
            if (writeToFile && account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            var mt940Parser = new MT940Parser(pending);
            mt940Parser.TraceSwiftStatementCallback = statement => TraceSwiftStatement(account, statement);

            if (string.IsNullOrEmpty(STA))
                return new List<SwiftStatement>();

            var memStream = new MemoryStream(Encoding.UTF8.GetBytes(STA));
            return mt940Parser.Deserialize(memStream).ToList();
        }

        /// <summary>
        /// Writes a MT940 statement into the library own folder for further use.
        /// </summary>
        /// <param name="sta">
        /// The raw text of the MT940 statement.
        /// </param>
        /// <param name="account">
        /// The account name.
        /// </param>
        public static void WriteToFile(string sta, string account)
        {
            string dir = FinTsGlobals.ProgramBaseDir;

            dir = Path.Combine(dir, "STA");

            string filename = Path.Combine(dir, Helper.MakeFilenameValid(account + "_" + DateTime.Now + ".STA"));

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // STA
            if (!File.Exists(filename))
            {
                using (File.Create(filename))
                { };

                File.AppendAllText(filename, sta);
            }
            else
                File.AppendAllText(filename, sta);
        }

        /// <summary>
        /// Deserializes a MT940 statement into a <see cref="SwiftStatement"/> class objects.
        /// </summary>
        /// <param name="sta">
        /// The raw text of the MT940 statement.
        /// </param>
        /// <param name="account">
        /// The account name. Only required when tracing is activated.
        /// </param>
        /// <param name="pending">
        /// If the Swift statements shall be marked as pending.
        /// </param>
        /// <returns>
        /// A enumerable object returning a list of swift statements from the MT940 statement.
        /// </returns>
        public static IEnumerable<SwiftStatement> Deserialize(string sta, string account = null, bool pending = false)
        {
            var mt940Parser = new MT940Parser(pending);
            mt940Parser.TraceSwiftStatementCallback = statement => TraceSwiftStatement(account, statement);
            var memStream = new MemoryStream(Encoding.UTF8.GetBytes(sta));
            return mt940Parser.Deserialize(memStream);
        }

        /// <summary>
        /// Deserializes a MT940 statement into a <see cref="SwiftStatement"/> class objects.
        /// </summary>
        /// <param name="stream">
        /// A stream providing the MT940 statement.
        /// </param>
        /// <param name="account">
        /// The account name. Only required when tracing is activated.
        /// </param>
        /// <param name="pending">
        /// If the Swift statements shall be marked as pending.
        /// </param>
        /// <returns>
        /// A enumerable object returning a list of swift statements from the MT940 statement.
        /// </returns>
        public static IEnumerable<SwiftStatement> Deserialize(Stream stream, string account = null, bool pending = false)
        {
            var mt940Parser = new MT940Parser(pending);
            mt940Parser.TraceSwiftStatementCallback = statement => TraceSwiftStatement(account, statement);
            return mt940Parser.Deserialize(stream);
        }

        private static void TraceSwiftStatement(string account, SwiftStatement statement)
        {
            if (!Trace.Enabled)
                return;

            string dir = FinTsGlobals.ProgramBaseDir;
            dir = Path.Combine(dir, "MT940");

            var ID = statement.Id;
            var AccountCode = statement.AccountCode;
            var BanksortCode = statement.BankCode;
            var Currency = statement.Currency;
            var StartDate = $"{statement.StartDate:d}";
            var StartBalance = statement.StartBalance.ToString();
            var EndDate = $"{statement.EndDate:d}";
            var EndBalance = statement.EndBalance.ToString();

            foreach (SwiftTransaction transaction in statement.SwiftTransactions)
            {
                var PartnerName = transaction.PartnerName;
                var AccountCode_ = transaction.AccountCode;
                var BankCode = transaction.BankCode;
                var Description = transaction.Description;
                var Text = transaction.Text;
                var TypeCode = transaction.TypeCode;
                var Amount = transaction.Amount.ToString();

                var UMS = "++STARTUMS++" + "ID: " + ID + " ' " +
                          "AccountCode: " + AccountCode + " ' " +
                          "BanksortCode: " + BanksortCode + " ' " +
                          "Currency: " + Currency + " ' " +
                          "StartDate: " + StartDate + " ' " +
                          "StartBalance: " + StartBalance + " ' " +
                          "EndDate: " + EndDate + " ' " +
                          "EndBalance: " + EndBalance + " ' " +
                          "PartnerName: " + PartnerName + " ' " +
                          "BankCode: " + BankCode + " ' " +
                          "Description: " + Description + " ' " +
                          "Text: " + Text + " ' " +
                          "TypeCode: " + TypeCode + " ' " +
                          "Amount: " + Amount + " ' " + "++ENDUMS++";

                string filename_ = Path.Combine(dir, Helper.MakeFilenameValid(account + "_" + DateTime.Now + ".MT940"));

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // MT940
                if (!File.Exists(filename_))
                {
                    using (File.Create(filename_))
                    { };

                    File.AppendAllText(filename_, UMS);
                }
                else
                    File.AppendAllText(filename_, UMS);
            }
        }
    }
}
