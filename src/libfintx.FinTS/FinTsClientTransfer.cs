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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using libfintx.FinTS.Data;
using libfintx.FinTS.Message;
using libfintx.Sepa;
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS
{
    public partial class FinTsClient
    {
        /// <summary>
        /// Transfer money - General method
        /// </summary>
        /// <param name="tanDialog"></param>
        /// <param name="receiverName">Name of the recipient</param>
        /// <param name="receiverIBAN">IBAN of the recipient</param>
        /// <param name="receiverBIC">BIC of the recipient</param>
        /// <param name="amount">Amount to transfer</param>
        /// <param name="purpose">Short description of the transfer (dt. Verwendungszweck)</param>      
        /// <param name="hirms">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <returns>
        /// Bank return codes
        /// </returns>
        public async Task<HBCIDialogResult> Transfer(TANDialog tanDialog, string receiverName, string receiverIBAN, string receiverBIC,
            decimal amount, string purpose, string hirms)
        {
            return await SubmitPaymentOrder(tanDialog, hirms, "HKCCS", (number, kti) => (HKCCS.Create(this, number, kti, receiverName, receiverIBAN, receiverBIC, amount, purpose), number));
        }

        /// <summary>
        /// Transfer money at a certain time - General method
        /// </summary>
        /// <param name="tanDialog"></param>
        /// <param name="receiverName">Name of the recipient</param>
        /// <param name="receiverIBAN">IBAN of the recipient</param>
        /// <param name="receiverBIC">BIC of the recipient</param>
        /// <param name="amount">Amount to transfer</param>
        /// <param name="purpose">Short description of the transfer (dt. Verwendungszweck)</param>      
        /// <param name="executionDay"></param>
        /// <param name="hirms">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <returns>
        /// Bank return codes
        /// </returns>
        public async Task<HBCIDialogResult> Transfer_Terminated(TANDialog tanDialog, string receiverName, string receiverIBAN, string receiverBIC,
            decimal amount, string purpose, DateTime executionDay, string hirms)
        {
            return await SubmitPaymentOrder(tanDialog, hirms, "HKCSE", (number, kti) => (HKCSE.Create(this, number, kti, receiverName, receiverIBAN, receiverBIC, amount, purpose, executionDay), number));
        }


        /// <summary>
        /// Collective transfer money - General method
        /// </summary>
        /// <param name="tanDialog"></param>
        /// <param name="painData"></param>
        /// <param name="numberOfTransactions"></param>
        /// <param name="totalAmount"></param>
        /// <param name="hirms">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <returns>
        /// Bank return codes
        /// </returns>
        public async Task<HBCIDialogResult> CollectiveTransfer(TANDialog tanDialog, List<Pain00100203CtData> painData,
            string numberOfTransactions, decimal totalAmount, string hirms)
        {
            return await SubmitPaymentOrder(tanDialog, hirms, "HKCCM", (number, kti) => (HKCCM.Create(this, number, kti, painData, numberOfTransactions, totalAmount), number));
        }

        /// <summary>
        /// Collective transfer money terminated - General method
        /// </summary>
        /// <param name="tanDialog"></param>
        /// <param name="painData"></param>
        /// <param name="numberOfTransactions"></param>
        /// <param name="totalAmount"></param>
        /// <param name="executionDay"></param>
        /// <param name="hirms">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <returns>
        /// Bank return codes
        /// </returns>
        public async Task<HBCIDialogResult> CollectiveTransfer_Terminated(TANDialog tanDialog, List<Pain00100203CtData> painData,
            string numberOfTransactions, decimal totalAmount, DateTime executionDay, string hirms)
        {
            return await SubmitPaymentOrder(tanDialog, hirms, "HKCME", (number, kti) => (HKCME.Create(this, number, kti, painData, numberOfTransactions, totalAmount, executionDay), number));
        }

        /// <summary>
        /// Get terminated transfers
        /// </summary>
        /// <returns>
        /// Banker's orders
        /// </returns>
        public async Task<HBCIDialogResult<List<TerminatedTransfer>>> GetTerminatedTransfers(TANDialog tanDialog)
        {
            var result = await InitializeConnection();
            if (result.HasError)
                return result.TypedResult<List<TerminatedTransfer>>();

            result = await ProcessSCA(result, tanDialog, true);
            if (!result.IsSuccess)
                return result.TypedResult<List<TerminatedTransfer>>();

            // Success
            string BankCode = await Transaction.HKCSB(this);
            result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            if (result.HasError)
                return result.TypedResult<List<TerminatedTransfer>>();

            result = await ProcessSCA(result, tanDialog);

            if (!result.IsSuccess)
                return result.TypedResult<List<TerminatedTransfer>>();

            BankCode = result.RawData;
            int startIdx = BankCode.IndexOf("HICSB");
            if (startIdx < 0)
                return result.TypedResult<List<TerminatedTransfer>>();

            var data = new List<TerminatedTransfer>();

            string BankCode_ = BankCode.Substring(startIdx);
            for (; ; )
            {
                var match = Regex.Match(BankCode_, @"HICSB.+?(?<xml><\?xml.+?</Document>)\+(?<orderid>.*?)(\+(?<deleteable>j|n))?(\+(?<modifiable>j|n))?'", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string xml = match.Groups["xml"].Value;
                    // xml ist UTF-8
                    xml = Converter.ConvertEncoding(xml, Encoding.GetEncoding("ISO-8859-1"), Encoding.UTF8);

                    string orderId = match.Groups["orderid"].Value;
                    bool? deleteable = null;
                    if (match.Groups["deleteable"].Success)
                        deleteable = match.Groups["deleteable"].Value.Equals("j", StringComparison.OrdinalIgnoreCase) ? true : false;

                    bool? modifiable = null;
                    if (match.Groups["modifiable"].Success)
                        modifiable = match.Groups["modifiable"].Value.Equals("j", StringComparison.OrdinalIgnoreCase) ? true : false;

                    var painData = Pain00100103CtData.Create(xml);

                    var item = new TerminatedTransfer(orderId, deleteable, modifiable, painData);
                    data.Add(item);
                }

                int endIdx = BankCode_.IndexOf("'");
                if (BankCode_.Length <= endIdx + 1)
                    break;

                BankCode_ = BankCode_.Substring(endIdx + 1);
                startIdx = BankCode_.IndexOf("HICSB");
                if (startIdx < 0)
                    break;
            }

            // Success
            return result.TypedResult(data);
        }

        public async Task<HBCIDialogResult> ModifyTerminatedTransfer(TANDialog tanDialog, string orderId, string receiverName, string receiverIBAN,
                string receiverBIC, decimal amount, string usage, DateTime executionDay, string hirms)
        {
            var result = await InitializeConnection();
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog, true);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!string.IsNullOrEmpty(hirms))
                HIRMS = hirms;

            string BankCode = await Transaction.HKCSA(this, orderId, receiverName, receiverIBAN, receiverBIC, amount, usage, executionDay);
            result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog);

            return result;
        }

        public async Task<HBCIDialogResult> DeleteTerminatedTransfer(TANDialog tanDialog, string orderId, string receiverName, string receiverIBAN,
                string receiverBIC, decimal amount, string usage, DateTime executionDay, string hirms)
        {
            var result = await InitializeConnection();
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog, true);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!string.IsNullOrEmpty(hirms))
                HIRMS = hirms;

            string BankCode = await Transaction.HKCSL(this, orderId, receiverName, receiverIBAN, receiverBIC, amount, usage, executionDay);
            result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog);

            return result;
        }

        /// <summary>
        /// Sends a payment order with HKTAN and completes the strong customer authentication. The account of the
        /// client is the one the bank reports in HISPA (see <see cref="GetPaymentOrderAccount"/>).
        /// </summary>
        /// <param name="buildOrder">Builds the order segments from the first segment number and the KTI of the account;
        /// returns them with the number of the last segment.</param>
        private async Task<HBCIDialogResult> SubmitPaymentOrder(TANDialog tanDialog, string hirms, string segmentId,
            Func<int, string, (string Segments, int LastSegmentNumber)> buildOrder)
        {
            var result = await InitializeConnection();
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog, true);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!string.IsNullOrEmpty(hirms))
                HIRMS = hirms;

            var kti = await GetPaymentOrderAccount(tanDialog);

            string BankCode = await SendPaymentOrder(buildOrder(Convert.ToInt16(SEG_NUM.Seg3), kti), segmentId);
            result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog);

            return result;
        }

        /// <summary>
        /// KTI of the own account for a payment order, exactly as the bank reports it in HISPA (HKSPA, as python-fints
        /// does before every order); some banks reject IBAN and BIC alone with 9210 "Keine gültige Kontoverbindung".
        /// IBAN and BIC of the connection details when the bank does not offer HKSPA or does not report the account.
        /// </summary>
        private async Task<string> GetPaymentOrderAccount(TANDialog tanDialog)
        {
            var fallback = ConnectionDetails.Iban + DEG.Separator + ConnectionDetails.Bic;
            if (HISPAS == 0)
                return fallback;

            string BankCode = await Transaction.HKSPA(this);
            var result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            if (!result.HasError)
                result = await ProcessSCA(result, tanDialog);
            if (result.HasError)
            {
                Logger.LogInformation("SEPA account connection (HKSPA) not received, using IBAN and BIC of the connection details.");
                return fallback;
            }

            var iban = ConnectionDetails.Iban?.Replace(" ", string.Empty);
            var account = Parse_SepaAccounts(result.RawData)
                .FirstOrDefault(a => string.Equals(a.AccountIban, iban, StringComparison.OrdinalIgnoreCase));
            return account == null ? fallback : KTI.Of(account);
        }

        /// <summary>
        /// Sends order segments (the first one numbered 3) with HKTAN (process 4) when the bank requires a TAN for
        /// <paramref name="segmentId"/>.
        /// </summary>
        private async Task<string> SendPaymentOrder((string Segments, int LastSegmentNumber) order, string segmentId)
        {
            var segments = order.Segments;
            SEGNUM = order.LastSegmentNumber;
            if (BPD.IsTANRequired(segmentId))
            {
                SEGNUM = order.LastSegmentNumber + 1;
                segments = HKTAN.Init_HKTAN(this, segments, segmentId);
            }

            string message = FinTSMessage.Create(this, HNHBS, HNHBK, segments, HIRMS);
            string response = await FinTSMessage.Send(this, message);

            Parse_Message(response);

            return response;
        }
    }
}
