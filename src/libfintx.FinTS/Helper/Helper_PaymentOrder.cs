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
using System.Linq;
using libfintx.FinTS.Data;
using libfintx.Sepa;

namespace libfintx.FinTS
{
    /// <summary>
    /// SEPA credit transfer orders: the pain message in the format the bank announces in HISPAS and the order segment.
    /// </summary>
    public static partial class Helper
    {
        /// <summary>
        /// pain message for one credit transfer in the format announced in HISPAS (pain.001.001.09 preferred).
        /// Instant payments always use pain.001.001.09.
        /// </summary>
        internal static (string Descriptor, string Message) CreatePainCreditTransfer(FinTsClient client, string receiverName,
            string receiverIBAN, string receiverBIC, decimal amount, string usage, DateTime executionDay,
            bool instantPayment = false, string messageId = null)
        {
            var c = client.ConnectionDetails;
            switch (instantPayment ? 4 : client.HISPAS_Pain)
            {
                case 4:
                    return (pain00100109.Descriptor, pain00100109.Create(c.AccountHolder, c.Iban, c.Bic, receiverName, receiverIBAN,
                        receiverBIC, amount, usage, executionDay, instantPayment, messageId, 140 - client.HISPAS_ReservedPurposeLength));
                case 1:
                    return ("urn:iso:std:iso:20022:tech:xsd:pain.001.001.03", pain00100103.Create(c.AccountHolder, c.Iban, c.Bic,
                        receiverName, receiverIBAN, receiverBIC, amount, usage, executionDay));
                case 2:
                    return ("urn:iso:std:iso:20022:tech:xsd:pain.001.002.03", pain00100203.Create(c.AccountHolder, c.Iban, c.Bic,
                        receiverName, receiverIBAN, receiverBIC, amount, usage, executionDay));
                default:
                    return ("urn:iso:std:iso:20022:tech:xsd:pain.001.003.03", pain00100303.Create(c.AccountHolder, c.Iban, c.Bic,
                        receiverName, receiverIBAN, receiverBIC, amount, usage, executionDay));
            }
        }

        /// <summary>pain message for a collective credit transfer, see <see cref="CreatePainCreditTransfer"/>.</summary>
        internal static (string Descriptor, string Message) CreatePainCollectiveCreditTransfer(FinTsClient client,
            List<Pain00100203CtData> painData, string numberOfTransactions, decimal totalAmount, DateTime executionDay,
            bool instantPayment = false, string messageId = null)
        {
            var c = client.ConnectionDetails;
            switch (instantPayment ? 4 : client.HISPAS_Pain)
            {
                case 4:
                    return (pain00100109.Descriptor, pain00100109.Create(c.AccountHolder, c.Iban, c.Bic, painData, executionDay,
                        instantPayment, messageId, 140 - client.HISPAS_ReservedPurposeLength));
                case 1:
                    return ("urn:iso:std:iso:20022:tech:xsd:pain.001.001.03", pain00100103.Create(c.AccountHolder, c.Iban, c.Bic,
                        painData, numberOfTransactions, totalAmount, executionDay));
                case 2:
                    return ("urn:iso:std:iso:20022:tech:xsd:pain.001.002.03", pain00100203.Create(c.AccountHolder, c.Iban, c.Bic,
                        painData, numberOfTransactions, totalAmount, executionDay));
                default:
                    return ("urn:iso:std:iso:20022:tech:xsd:pain.001.003.03", pain00100303.Create(c.AccountHolder, c.Iban, c.Bic,
                        painData, numberOfTransactions, totalAmount, executionDay));
            }
        }

        /// <summary>
        /// Order segment "ID:number:version+KTI+[elements]+descriptor+@length@pain'[+trailing elements]". The pain
        /// generators end with the segment terminator, which is not part of the binary data.
        /// </summary>
        internal static string PaymentOrderSegment(string segmentId, int version, int segmentNumber, string kti,
            IEnumerable<string> elementsBeforeDescriptor, (string Descriptor, string Message) pain,
            IEnumerable<string> elementsAfterPain = null)
        {
            var message = pain.Message.EndsWith("'") ? pain.Message.Substring(0, pain.Message.Length - 1) : pain.Message;
            var elements = new List<string> { segmentId + DEG.Separator + segmentNumber + DEG.Separator + version, kti };
            elements.AddRange(elementsBeforeDescriptor ?? Enumerable.Empty<string>());
            elements.Add(EscapeEdifactString(pain.Descriptor));
            elements.Add("@" + message.Length + "@" + message);
            elements.AddRange(elementsAfterPain ?? Enumerable.Empty<string>());
            var seg = new SEG();
            return string.Join(seg.Delimiter, elements) + seg.Terminator;
        }
    }
}
