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
using libfintx.FinTS.Data;
using libfintx.Sepa;

namespace libfintx.FinTS
{
    /// <summary>
    /// SEPA instant payment (SEPA-Instant Payment Zahlung, "FinTS 3.0 Messages Geschäftsvorfälle" 2022-04-15, C.10.2.9).
    /// </summary>
    public static class HKIPZ
    {
        /// <summary>
        /// HKIPZ v1 [account, descriptor, pain], v2 additionally [conversion to a standard credit transfer allowed], sent
        /// only when the bank allows the conversion. The pain message is pain.001.001.09 with local instrument INST.
        /// </summary>
        internal static string Create(FinTsClient client, int segmentNumber, int version, string kti, string receiverName,
            string receiverIBAN, string receiverBIC, decimal amount, string usage, bool? allowConversion, string messageId = null,
            string debtorBic = null)
        {
            var pain = Helper.CreatePainCreditTransfer(client, receiverName, receiverIBAN, receiverBIC, amount, usage,
                new DateTime(1999, 1, 1), instantPayment: true, messageId: messageId, debtorBic: debtorBic);
            var after = version >= 2 && allowConversion.HasValue ? new[] { allowConversion.Value ? "J" : "N" } : null;
            return Helper.PaymentOrderSegment("HKIPZ", version, segmentNumber, kti, null, pain, after);
        }
    }

    /// <summary>
    /// SEPA instant payment collective order (SEPA-Instant Payment Sammelzahlung, C.10.3.5).
    /// </summary>
    public static class HKIPM
    {
        /// <summary>HKIPM v1 [account, total, descriptor, pain], v2 additionally [conversion allowed].</summary>
        internal static string Create(FinTsClient client, int segmentNumber, int version, string kti, List<Pain00100203CtData> painData,
            string numberOfTransactions, decimal totalAmount, bool? allowConversion, string messageId = null, string debtorBic = null)
        {
            var pain = Helper.CreatePainCollectiveCreditTransfer(client, painData, numberOfTransactions, totalAmount,
                new DateTime(1999, 1, 1), instantPayment: true, messageId: messageId, debtorBic: debtorBic);
            var after = version >= 2 && allowConversion.HasValue ? new[] { allowConversion.Value ? "J" : "N" } : null;
            return Helper.PaymentOrderSegment("HKIPM", version, segmentNumber, kti,
                new[] { Converter.ToFinTsAmount(totalAmount) + DEG.Separator + "EUR" }, pain, after);
        }
    }

    /// <summary>Status request of an instant payment (HKIPS) or of an instant payment collective order (HKISS).</summary>
    public static class HKIPS
    {
        /// <summary>
        /// HKIPS [account, descriptor, order id]; HKISS [account, supported payment status reports, descriptor, order id].
        /// </summary>
        internal static string Create(bool collective, int segmentNumber, int version, string kti, string statusReportFormats, string orderId)
        {
            var seg = new SEG();
            var elements = new List<string> { (collective ? "HKISS" : "HKIPS") + DEG.Separator + segmentNumber + DEG.Separator + Math.Max(1, version), kti };
            if (collective)
            {
                var formats = (statusReportFormats ?? string.Empty).Split(';');
                elements.Add(string.Join(DEG.Separator, Array.ConvertAll(formats, f => Helper.EscapeEdifactString(f.Trim()))));
            }
            elements.Add(Helper.EscapeEdifactString(pain00100109.Descriptor));
            elements.Add(Helper.EscapeEdifactString(orderId));
            return string.Join(seg.Delimiter, elements) + seg.Terminator;
        }
    }
}
