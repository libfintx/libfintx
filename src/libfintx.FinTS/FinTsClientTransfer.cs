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
            return await SubmitPaymentOrder(tanDialog, hirms, "HKCCS", new[] { (receiverName, receiverIBAN) },
                (number, kti, messageId) => (HKCCS.Create(this, number, kti, receiverName, receiverIBAN, receiverBIC, amount, purpose, messageId), number));
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
            return await SubmitPaymentOrder(tanDialog, hirms, "HKCSE", new[] { (receiverName, receiverIBAN) },
                (number, kti, messageId) => (HKCSE.Create(this, number, kti, receiverName, receiverIBAN, receiverBIC, amount, purpose, executionDay, messageId), number));
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
            // A collective order with one transaction goes as single transfer when the bank does not allow it (HIVPPS)
            if (painData?.Count == 1 && BPD?.HIVPPS?.CollectiveWithOneTransactionAllowed == false)
                return await Transfer(tanDialog, painData[0].Receiver, painData[0].ReceiverIban, painData[0].ReceiverBic, painData[0].Amount, painData[0].Usage, hirms);

            CheckMaximumTransactions("HKCCM", "HICCMS", painData.Count);
            return await SubmitPaymentOrder(tanDialog, hirms, "HKCCM", painData.Select(p => (p.Receiver, p.ReceiverIban)).ToList(),
                (number, kti, messageId) => (HKCCM.Create(this, number, kti, painData, numberOfTransactions, totalAmount, messageId), number));
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
            if (painData?.Count == 1 && BPD?.HIVPPS?.CollectiveWithOneTransactionAllowed == false)
                return await Transfer_Terminated(tanDialog, painData[0].Receiver, painData[0].ReceiverIban, painData[0].ReceiverBic, painData[0].Amount, painData[0].Usage, executionDay, hirms);

            CheckMaximumTransactions("HKCME", "HICMES", painData.Count);
            return await SubmitPaymentOrder(tanDialog, hirms, "HKCME", painData.Select(p => (p.Receiver, p.ReceiverIban)).ToList(),
                (number, kti, messageId) => (HKCME.Create(this, number, kti, painData, numberOfTransactions, totalAmount, executionDay, messageId), number));
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
        /// client is the one the bank reports in HISPA (see <see cref="GetPaymentOrderAccount"/>). When the bank lists
        /// the order in HIVPPS, the verification of payee comes first (see <see cref="SubmitWithVerificationOfPayee"/>).
        /// </summary>
        /// <param name="payees">Name and IBAN of each transfer, in pain order.</param>
        /// <param name="buildOrder">Builds the order segments from the first segment number, the KTI of the account and
        /// the pain message id; returns them with the number of the last segment.</param>
        private async Task<HBCIDialogResult> SubmitPaymentOrder(TANDialog tanDialog, string hirms, string segmentId,
            IReadOnlyList<(string Name, string Iban)> payees, Func<int, string, string, (string Segments, int LastSegmentNumber)> buildOrder,
            bool instantPayment = false)
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
            var messageId = pain00100109.NewMessageId();
            int first = Convert.ToInt16(SEG_NUM.Seg3);

            var vop = BPD?.HIVPPS;
            if (vop != null && vop.PaymentOrders.Contains(segmentId))
            {
                var withEndToEndIds = payees.Select((p, i) => (p.Name, p.Iban, pain00100109.EndToEndId(messageId, i))).ToList();
                return await SubmitWithVerificationOfPayee(tanDialog, segmentId, vop, withEndToEndIds, buildOrder(first + 1, kti, messageId), instantPayment);
            }

            string BankCode = await SendPaymentOrder(buildOrder(first, kti, messageId), segmentId);
            result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog);

            return result;
        }

        /// <summary>
        /// Verification of payee after "FinTS 3.0 Messages Geschäftsvorfälle VOP" 1.02, appendix E.8.1 (PIN/TAN, single
        /// signature): check HKVPP + order + HKTAN, answer HIVPP. Controlled only by return values: 3945 = TAN
        /// invalidated, after the user's consent HKVPA (VOP id) + the same order + a new HKTAN; 3091 = no confirmation
        /// needed, answer the challenge; otherwise HKVPA together with the TAN (figure 3). 3040 = fetch further results.
        /// The user sees the result through <see cref="TANDialog.ConfirmVerificationOfPayeeAsync"/>.
        /// </summary>
        /// <param name="order">The order segments numbered from 4 (HKVPP is 3); the same bytes are sent again after 3945.</param>
        private async Task<HBCIDialogResult> SubmitWithVerificationOfPayee(TANDialog tanDialog, string segmentId, HIVPPS vop,
            IReadOnlyList<(string Name, string Iban, string EndToEndId)> payees, (string Segments, int LastSegmentNumber) order,
            bool instantPayment)
        {
            int first = Convert.ToInt16(SEG_NUM.Seg3);
            string BankCode = await SendPaymentOrder((HKVPP.Create(first, vop) + order.Segments, order.LastSegmentNumber), segmentId);
            var result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            result.IgnoredSegmentReferences.Add(first);
            if (result.HasError)
                return result;

            // 3945 and 3091 come with the first answer (reference HKTAN): keep the codes of all answers
            var codes = new HashSet<string>(result.Messages.Select(m => m.Code));
            var answer = VerificationOfPayee.ParseAnswer(BankCode);
            var reports = new List<byte[]>();
            if (answer?.PaymentStatusReport != null)
                reports.Add(answer.PaymentStatusReport);
            var restart = result.Messages.FirstOrDefault(m => m.Code == "3040")?.Parameters.FirstOrDefault();

            // Large results come in parts: HKVPP alone with polling id and restart point until the VOP id comes
            for (int round = 0; answer != null && answer.VopId == null && !string.IsNullOrEmpty(restart) && round < 30; round++)
            {
                await Task.Delay(Math.Min(60, Math.Max(1, answer.WaitSeconds ?? 2)) * 1000);
                var pollingId = answer.PollingId;
                BankCode = await SendPaymentOrder((HKVPP.Create(first, vop, pollingId, restart), first), null);
                var next = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
                if (next.HasError)
                    return next;

                codes.UnionWith(next.Messages.Select(m => m.Code));
                restart = next.Messages.FirstOrDefault(m => m.Code == "3040")?.Parameters.FirstOrDefault();
                var nextAnswer = VerificationOfPayee.ParseAnswer(BankCode);
                if (nextAnswer?.PaymentStatusReport != null)
                    reports.Add(nextAnswer.PaymentStatusReport);
                if (nextAnswer != null)
                {
                    nextAnswer.PollingId ??= pollingId;
                    nextAnswer.Explanation ??= answer.Explanation;
                    answer = nextAnswer;
                }
            }
            answer ??= new VerificationOfPayeeAnswer();

            var confirm = codes.Contains("3945");
            var withoutExecutionOrder = codes.Contains("3091");
            var alreadyExecuted = !confirm && !result.IsSCARequired && result.GetOrderStatus(instantPayment) == OrderStatus.Accepted;
            var withTan = !confirm && !withoutExecutionOrder && !alreadyExecuted && answer.VopId != null && result.IsSCARequired;

            var check = VerificationOfPayee.Evaluate(answer, payees, reports, vop.ExplanationStructured, false, codes.Contains("0025"));
            check.ConfirmationRequired = confirm || (withTan && !check.AllMatch);
            if (tanDialog != null)
                tanDialog.VerificationOfPayee = check;

            var consent = tanDialog?.ConfirmVerificationOfPayeeAsync != null
                ? await tanDialog.ConfirmVerificationOfPayeeAsync(check)
                : !check.ConfirmationRequired;

            if (alreadyExecuted)
            {
                // Executed without TAN (e.g. 3076 small amount): it cannot be stopped any more
                Logger.LogInformation("The bank executed the order without TAN, it cannot be cancelled after the verification of payee.");
                return result;
            }

            if (!consent)
            {
                // Not released; a decoupled approval already started stays valid and has to be rejected in the app (3955)
                Logger.LogInformation("Order not released after the verification of payee.");
                return result;
            }

            if (confirm)
            {
                if (answer.VopId == null)
                {
                    Logger.LogWarning("The bank sent no VOP id for the verification of payee, the order cannot be confirmed.");
                    return result;
                }

                BankCode = await SendPaymentOrder((HKVPA.Create(first, answer.VopId.Text) + order.Segments, order.LastSegmentNumber), segmentId);
                result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
                result.IgnoredSegmentReferences.Add(first);
                if (result.HasError)
                    return result;

                return await ProcessSCA(result, tanDialog);
            }

            if (withTan)
            {
                if (HITANS < 6)
                {
                    Logger.LogWarning($"HKTAN version {HITANS}: the confirmation of the verification of payee (HKVPA) cannot be sent with the TAN.");
                    return result;
                }

                var vopId = answer.VopId.Text;
                return await ProcessSCA(result, tanDialog, extraSegments: number => (HKVPA.Create(number, vopId), number));
            }

            return await ProcessSCA(result, tanDialog);
        }

        /// <summary>
        /// Throws when a collective order has more transactions than the bank accepts: the maximum of the order
        /// parameters (e.g. HICCMS) and of the verification of payee (HIVPPS), element 1 of the parameter group.
        /// </summary>
        private void CheckMaximumTransactions(string segmentId, string parameterSegment, int count)
        {
            var parameters = BPD?.SegmentList?.Where(s => s.Name == parameterSegment).OrderByDescending(s => s.Version).FirstOrDefault();
            if (parameters?.DataElements.Count > 3 && int.TryParse(Helper.SplitDataElementGroup(parameters.DataElements[3])[0], out var max) && max > 0 && count > max)
                throw new ArgumentException($"The bank accepts at most {max} transactions in {segmentId} ({count} given).");

            var vop = BPD?.HIVPPS;
            if (vop != null && vop.PaymentOrders.Contains(segmentId) && vop.MaxTransactions > 0 && count > vop.MaxTransactions)
                throw new ArgumentException($"The bank checks at most {vop.MaxTransactions} payees per order ({count} given).");
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
            if (!string.IsNullOrEmpty(segmentId) && BPD.IsTANRequired(segmentId))
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
