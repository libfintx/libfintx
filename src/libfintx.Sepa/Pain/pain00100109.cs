/*
 *
 *  This file is part of libfintx.
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
using System.Linq;
using System.Security;
using System.Text;
using libfintx.Sepa.Helper;

namespace libfintx.Sepa
{
    /// <summary>
    /// SEPA credit transfer initiation pain.001.001.09 (DFÜ-Abkommen Anlage 3 V3.9, chapter 2.2.1; mandatory for
    /// German banks from 2026-11-15). One payment information block, as FinTS allows. An instant payment
    /// (SCT Inst, chapter 2.2.4) carries the local instrument INST on payment information level. The execution date
    /// 1999-01-01 means "as soon as possible".
    /// </summary>
    public static class pain00100109
    {
        public const string Descriptor = "urn:iso:std:iso:20022:tech:xsd:pain.001.001.09";

        /// <summary>
        /// Single credit transfer. <paramref name="messageId"/> (max. 30 characters) gives the end to end id "messageId-1"
        /// and is generated when empty; <paramref name="maxUsageLength"/> is 140 less the remittance information positions
        /// the bank reserves in HISPAS. Returns the XML message followed by the FinTS segment terminator, like the other
        /// pain generators.
        /// </summary>
        public static string Create(string Accountholder, string AccountholderIBAN, string AccountholderBIC, string Receiver,
            string ReceiverIBAN, string ReceiverBIC, decimal Amount, string Usage, DateTime ExecutionDay,
            bool InstantPayment = false, string messageId = null, int maxUsageLength = 140)
        {
            var transaction = new Pain00100203CtData
            {
                Receiver = Receiver,
                ReceiverIban = ReceiverIBAN,
                ReceiverBic = ReceiverBIC,
                Amount = Amount,
                Usage = Usage,
            };
            return Create(Accountholder, AccountholderIBAN, AccountholderBIC, new List<Pain00100203CtData> { transaction },
                ExecutionDay, InstantPayment, messageId, maxUsageLength);
        }

        /// <summary>Collective credit transfer; the end to end ids are "messageId-1", "messageId-2", ... in list order.</summary>
        public static string Create(string Accountholder, string AccountholderIBAN, string AccountholderBIC,
            List<Pain00100203CtData> PainData, DateTime ExecutionDay, bool InstantPayment = false, string messageId = null,
            int maxUsageLength = 140)
        {
            if (PainData == null || PainData.Count == 0)
                throw new ArgumentException("No credit transfers.", nameof(PainData));

            messageId = string.IsNullOrEmpty(messageId) ? NewMessageId() : Truncate(messageId, 30);
            var sum = Amount(PainData.Sum(p => p.Amount));
            var count = PainData.Count.ToString(CultureInfo.InvariantCulture);

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<Document xmlns=\"" + Descriptor + "\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"" + Descriptor + " pain.001.001.09.xsd\">");
            sb.Append("<CstmrCdtTrfInitn>");
            sb.Append("<GrpHdr>");
            sb.Append("<MsgId>" + messageId + "</MsgId>");
            sb.Append("<CreDtTm>" + DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + "</CreDtTm>");
            sb.Append("<NbOfTxs>" + count + "</NbOfTxs>");
            sb.Append("<CtrlSum>" + sum + "</CtrlSum>");
            sb.Append("<InitgPty><Nm>" + Text(Accountholder, 70) + "</Nm></InitgPty>");
            sb.Append("</GrpHdr>");
            sb.Append("<PmtInf>");
            sb.Append("<PmtInfId>" + messageId + "</PmtInfId>");
            sb.Append("<PmtMtd>TRF</PmtMtd>");
            sb.Append("<NbOfTxs>" + count + "</NbOfTxs>");
            sb.Append("<CtrlSum>" + sum + "</CtrlSum>");
            sb.Append("<PmtTpInf><SvcLvl><Cd>SEPA</Cd></SvcLvl>");
            if (InstantPayment)
                sb.Append("<LclInstrm><Cd>INST</Cd></LclInstrm>");
            sb.Append("</PmtTpInf>");
            sb.Append("<ReqdExctnDt><Dt>" + ExecutionDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "</Dt></ReqdExctnDt>");
            sb.Append("<Dbtr><Nm>" + Text(Accountholder, 70) + "</Nm></Dbtr>");
            sb.Append("<DbtrAcct><Id><IBAN>" + Iban(AccountholderIBAN) + "</IBAN></Id></DbtrAcct>");
            sb.Append("<DbtrAgt><FinInstnId>");
            sb.Append(string.IsNullOrWhiteSpace(AccountholderBIC) ? "<Othr><Id>NOTPROVIDED</Id></Othr>" : "<BICFI>" + Iban(AccountholderBIC) + "</BICFI>");
            sb.Append("</FinInstnId></DbtrAgt>");
            sb.Append("<ChrgBr>SLEV</ChrgBr>");

            for (int i = 0; i < PainData.Count; i++)
            {
                var t = PainData[i];
                sb.Append("<CdtTrfTxInf>");
                sb.Append("<PmtId><EndToEndId>" + EndToEndId(messageId, i) + "</EndToEndId></PmtId>");
                sb.Append("<Amt><InstdAmt Ccy=\"EUR\">" + Amount(t.Amount) + "</InstdAmt></Amt>");
                if (!string.IsNullOrWhiteSpace(t.ReceiverBic))
                    sb.Append("<CdtrAgt><FinInstnId><BICFI>" + Iban(t.ReceiverBic) + "</BICFI></FinInstnId></CdtrAgt>");
                sb.Append("<Cdtr><Nm>" + Text(t.Receiver, 70) + "</Nm></Cdtr>");
                sb.Append("<CdtrAcct><Id><IBAN>" + Iban(t.ReceiverIban) + "</IBAN></Id></CdtrAcct>");
                if (!string.IsNullOrWhiteSpace(t.Usage))
                    sb.Append("<RmtInf><Ustrd>" + Text(t.Usage, Math.Min(140, Math.Max(1, maxUsageLength))) + "</Ustrd></RmtInf>");
                sb.Append("</CdtTrfTxInf>");
            }

            sb.Append("</PmtInf></CstmrCdtTrfInitn></Document>'");
            return sb.ToString();
        }

        /// <summary>End to end id of the transaction at <paramref name="index"/> (0 based) of a message.</summary>
        public static string EndToEndId(string messageId, int index) => messageId + "-" + (index + 1).ToString(CultureInfo.InvariantCulture);

        /// <summary>A new message id (max. 30 characters).</summary>
        public static string NewMessageId() =>
            DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        private static string Amount(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

        private static string Iban(string value) => (value ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();

        // Valid SEPA characters first, then cut, then XML escaping (an entity must not be cut in half)
        private static string Text(string value, int length) =>
            SecurityElement.Escape(Truncate(SepaHelper.ConvertToValidSepaString(value ?? string.Empty).Trim(), length));

        private static string Truncate(string value, int length) => value.Length <= length ? value : value.Substring(0, length);
    }
}
