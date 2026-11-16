/*	
 * 	
 *  This file is part of libfintx.
 *  
 *  Copyright (C) 2016 - 2021 Torsten Klinger
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using libfintx.FinTS.Camt;
using libfintx.FinTS.Camt.Camt052;
using libfintx.FinTS.Camt.Camt053;
using libfintx.Globals;
using libfintx.Swift;

namespace libfintx.FinTS
{
    public partial class FinTsClient
    {
        /// <summary>
        /// Account transactions in SWIFT-format
        /// </summary>
        /// <param name="tanDialog"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="saveMt940File"></param>
        /// <returns>
        /// Transactions
        /// </returns>
        public async Task<HBCIDialogResult<List<SwiftStatement>>> Transactions(TANDialog tanDialog, DateTime? startDate = null, DateTime? endDate = null, bool saveMt940File = false)
        {
            var result = await InitializeConnection();
            if (result.HasError)
                return result.TypedResult<List<SwiftStatement>>();

            result = await ProcessSCA(result, tanDialog);
            if (result.HasError || result.IsAuthenticationPending)
                return result.TypedResult<List<SwiftStatement>>();

            string startDateStr = startDate?.ToString("yyyyMMdd");
            string endDateStr = endDate?.ToString("yyyyMMdd");

            // Success
            string BankCode = await Transaction.HKKAZ(this, startDateStr, endDateStr, null);
            result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            if (result.HasError)
                return result.TypedResult<List<SwiftStatement>>();

            result = await ProcessSCA(result, tanDialog);
            if (result.HasError || result.IsAuthenticationPending)
                return result.TypedResult<List<SwiftStatement>>();

            BankCode = result.RawData;
            var TransactionsMt940 = new StringBuilder();
            var TransactionsMt942 = new StringBuilder();

            Regex regex = new Regex(@"HIKAZ:.+?@\d+@(?<mt940>.+?)(\+@\d+@(?<mt942>.+?))?('{1,2}H[A-Z]{4}:\d+:\d+)", RegexOptions.Singleline);
            Match match = regex.Match(BankCode);
            if (match.Success)
            {
                TransactionsMt940.Append(match.Groups["mt940"].Value);
                TransactionsMt942.Append(match.Groups["mt942"].Value);
            }

            string BankCode_ = BankCode;
            while (BankCode_.Contains("+3040::"))
            {
                Parse_Message(BankCode_);

                string Startpoint = Parse_Transactions_Startpoint(BankCode_);

                BankCode_ = await Transaction.HKKAZ(this, startDateStr, endDateStr, Startpoint);
                result = new HBCIDialogResult(Parse_BankCode(BankCode_), BankCode_);
                if (result.HasError)
                    return result.TypedResult<List<SwiftStatement>>();

                result = await ProcessSCA(result, tanDialog);
                if (result.HasError || result.IsAuthenticationPending)
                    return result.TypedResult<List<SwiftStatement>>();

                BankCode_ = result.RawData;
                match = regex.Match(BankCode_);
                if (match.Success)
                {
                    TransactionsMt940.Append(match.Groups["mt940"].Value);
                    TransactionsMt942.Append(match.Groups["mt942"].Value);
                }
            }

            var swiftStatements = new List<SwiftStatement>();

            swiftStatements.AddRange(MT940.Deserialize(TransactionsMt940.ToString(), loggerFactory: _loggerFactory));
            if (saveMt940File)
            {
                WriteMT940ToFile(TransactionsMt940.ToString(), ConnectionDetails.Account);
            }
            swiftStatements.AddRange(MT940.Deserialize(TransactionsMt942.ToString(), pending: true, _loggerFactory));
            if (saveMt940File)
            {
                WriteMT940ToFile(TransactionsMt942.ToString(), ConnectionDetails.Account);
            }

            return result.TypedResult(swiftStatements);
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
        // ReSharper disable once InconsistentNaming
        private static void WriteMT940ToFile(string sta, string account)
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
                { }

                File.AppendAllText(filename, sta);
            }
            else
                File.AppendAllText(filename, sta);
        }

        /// <summary>
        /// Account transactions in camt format
        /// </summary>
        /// <param name="camtVers"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="tanDialog"></param>
        /// <param name="saveCamtFile"></param>
        /// <returns>
        /// Transactions
        /// </returns>
        public async Task<HBCIDialogResult<List<CamtStatement>>> Transactions_camt(TANDialog tanDialog, CamtVersion camtVers,
            DateTime? startDate = null, DateTime? endDate = null, bool saveCamtFile = false)
        {
            var result = await InitializeConnection();
            if (result.HasError)
                return result.TypedResult<List<CamtStatement>>();

            result = await ProcessSCA(result, tanDialog);
            if (result.HasError || result.IsAuthenticationPending)
                return result.TypedResult<List<CamtStatement>>();

            string startDateStr = startDate?.ToString("yyyyMMdd");
            string endDateStr = endDate?.ToString("yyyyMMdd");

            // Success
            string BankCode = await Transaction.HKCAZ(this, startDateStr, endDateStr, null, camtVers);
            result = new HBCIDialogResult<List<CamtStatement>>(Parse_BankCode(BankCode), BankCode);
            if (result.HasError)
                return result.TypedResult<List<CamtStatement>>();

            result = await ProcessSCA(result, tanDialog);
            if (result.HasError || result.IsAuthenticationPending)
                return result.TypedResult<List<CamtStatement>>();

            BankCode = result.RawData;
            var statements = new List<CamtStatement>();

            ReadCamtDocuments(BankCode, camtVers, saveCamtFile, statements);

            string BankCode_ = BankCode;
            while (BankCode_.Contains("+3040::"))
            {
                Parse_Message(BankCode_);

                string Startpoint = Parse_Transactions_Startpoint(BankCode_);

                BankCode_ = await Transaction.HKCAZ(this, startDateStr, endDateStr, Startpoint, camtVers);
                result = new HBCIDialogResult<List<CamtStatement>>(Parse_BankCode(BankCode_), BankCode_);
                if (result.HasError)
                    return result.TypedResult<List<CamtStatement>>();

                result = await ProcessSCA(result, tanDialog);
                if (result.HasError || result.IsAuthenticationPending)
                    return result.TypedResult<List<CamtStatement>>();

                BankCode_ = result.RawData;
                ReadCamtDocuments(BankCode_, camtVers, saveCamtFile, statements);
            }

            return result.TypedResult(statements);
        }

        /// <summary>
        /// Reads every camt document of a bank answer into <paramref name="statements"/>. An answer
        /// can hold several documents: the booked reports and the one of pending transactions.
        /// Some banks send the XML without its declaration (Hypo Vereinsbank).
        /// </summary>
        private void ReadCamtDocuments(string bankCode, CamtVersion camtVers, bool saveCamtFile, List<CamtStatement> statements)
        {
            var encoding = Encoding.GetEncoding("ISO-8859-1");
            string rest = bankCode;
            while (true)
            {
                int start = rest.IndexOf("<?xml version");
                if (start < 0)
                    start = rest.IndexOf("<Document");
                int end = rest.IndexOf("</Document>");
                if (start < 0 || end < start)
                    return;
                end += "</Document>".Length;

                string camt = rest.Substring(start, end - start);
                if (!camt.StartsWith("<?xml"))
                    camt = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + camt;

                switch (camtVers)
                {
                    case CamtVersion.Camt052:
                        if (saveCamtFile)
                            Camt052File.Save(ConnectionDetails.Account, camt, encoding);
                        var camt052Parser = new Camt052Parser(_loggerFactory);
                        camt052Parser.ProcessDocument(camt, encoding);
                        statements.AddRange(camt052Parser.statements);
                        break;
                    case CamtVersion.Camt053:
                        if (saveCamtFile)
                            Camt053File.Save(ConnectionDetails.Account, camt, encoding);
                        var camt053Parser = new Camt053Parser(_loggerFactory);
                        camt053Parser.ProcessDocument(camt, encoding);
                        statements.AddRange(camt053Parser.statements);
                        break;
                }

                rest = rest.Substring(end);
            }
        }

        /// <summary>
        /// Account transactions in simplified libfintx-format
        /// </summary>
        /// <param name="tanDialog"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns>
        /// Transactions
        /// </returns>
        public async Task<HBCIDialogResult<List<AccountTransaction>>> TransactionsSimple(TANDialog tanDialog, DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = await Transactions(tanDialog, startDate, endDate);
            if (!result.IsSuccess)
                return result.TypedResult<List<AccountTransaction>>();

            var transactionList = new List<AccountTransaction>();
            foreach (var swiftStatement in result.Data)
            {
                foreach (var swiftTransaction in swiftStatement.SwiftTransactions)
                {
                    transactionList.Add(new AccountTransaction()
                    {
                        OwnerAccount = swiftStatement.AccountCode,
                        OwnerBankCode = swiftStatement.BankCode,
                        PartnerBic = swiftTransaction.BankCode,
                        PartnerIban = swiftTransaction.AccountCode,
                        PartnerName = swiftTransaction.PartnerName,
                        RemittanceText = swiftTransaction.Description,
                        TransactionType = swiftTransaction.Text,
                        TransactionDate = swiftTransaction.InputDate,
                        ValueDate = swiftTransaction.ValueDate
                    });
                }
            }

            return result.TypedResult(transactionList);
        }
    }
}
