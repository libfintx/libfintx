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
using libfintx.FinTS.BankParameterData;

namespace libfintx.FinTS
{
    /// <summary>State of an instant payment as reported by the bank ("Status SEPA-Auftrag" and return values).</summary>
    public enum InstantPaymentStatus
    {
        /// <summary>No state reported; check the account statement.</summary>
        Unknown,

        /// <summary>Accepted, the final result is still open (status 1, 3, 4 or 3045/3047).</summary>
        Pending,

        /// <summary>Executed, the money is available to the payee (status 7).</summary>
        Executed,

        /// <summary>Not possible as instant payment, passed on as standard SEPA credit transfer (3270).</summary>
        ConvertedToCreditTransfer,

        /// <summary>Failed or rejected (status 2, 5, 6, 8, 9 or 3042/9xxx).</summary>
        Rejected,
    }

    /// <summary>Result of an instant payment or of its status request.</summary>
    public class InstantPaymentResult
    {
        public InstantPaymentResult(string orderId, InstantPaymentStatus status, string statusCode, bool collective = false)
        {
            OrderId = orderId;
            Status = status;
            StatusCode = statusCode;
            Collective = collective;
        }

        /// <summary>
        /// Sent as collective order (HKIPM): ask for its status with <c>collective: true</c>. A collective order with one
        /// transaction goes as single order when the bank does not allow it.
        /// </summary>
        public bool Collective { get; }

        /// <summary>Order id of the bank (Auftragsidentifikation), needed for the status request.</summary>
        public string OrderId { get; }

        public InstantPaymentStatus Status { get; }

        /// <summary>
        /// "Status SEPA-Auftrag": 1 scheduled, 2 rejected by the first collecting agent, 3 in process, 4 processed by the
        /// creditor side, 5 R transaction, 6 failed, 7 executed and available to the payee, 8 rejected by the payer's
        /// payment service provider, 9 rejected by the payee's payment service provider.
        /// </summary>
        public string StatusCode { get; }
    }

    /// <summary>Bank parameters of an instant payment order (HIIPZS or HIIPMS) and of its status request (HIIPSS or HIISSS).</summary>
    internal sealed class InstantPaymentParameters
    {
        public int Version { get; set; }
        public bool ConversionAllowed { get; set; }
        public int MaxTransactions { get; set; }
        public List<string> Formats { get; set; } = new List<string>();
        public int StatusVersion { get; set; }
        public int MinimumWaitSeconds { get; set; }
        public string StatusReportFormats { get; set; }

        /// <summary>
        /// Parameters from the BPD; <c>null</c> when the bank does not offer the order ("FinTS 3.0 Messages
        /// Geschäftsvorfälle" 2022-04-15, C.10.2.9 and C.10.3.5). Parameter group: HIIPZS v1 [purpose codes, formats],
        /// v2 [conversion allowed, purpose codes, formats]; HIIPMS v1 [max. transactions, total required, purpose codes,
        /// formats], v2 [max. transactions, total required, conversion allowed, purpose codes, formats]. Status: HIIPSS
        /// [minimum wait in seconds, formats], HIISSS [minimum wait in MINUTES, payment status report formats, formats].
        /// </summary>
        public static InstantPaymentParameters Read(BPD bpd, bool collective)
        {
            var order = Latest(bpd, collective ? "HIIPMS" : "HIIPZS");
            if (order == null)
                return null;

            var p = Group(order);
            string At(int i) => i < p.Count ? p[i] : string.Empty;
            var result = new InstantPaymentParameters { Version = order.Version };
            int formatsFrom;
            if (collective)
            {
                result.MaxTransactions = int.TryParse(At(0), out var max) ? max : 0;
                result.ConversionAllowed = order.Version >= 2 && At(2) == "J";
                formatsFrom = order.Version >= 2 ? 4 : 3;
            }
            else
            {
                result.ConversionAllowed = order.Version >= 2 && At(0) == "J";
                formatsFrom = order.Version >= 2 ? 2 : 1;
            }
            result.Formats = p.Skip(formatsFrom).Where(f => f.Length > 0).ToList();

            var status = Latest(bpd, collective ? "HIISSS" : "HIIPSS");
            if (status != null)
            {
                var s = Group(status);
                result.StatusVersion = status.Version;
                var wait = s.Count > 0 && int.TryParse(s[0], out var w) ? (collective ? w * 60 : w) : 1;
                result.MinimumWaitSeconds = Math.Max(1, wait);
                result.StatusReportFormats = collective && s.Count > 1 && s[1].Length > 0 ? s[1] : null;
            }
            return result;
        }

        private static Data.Segment.Segment Latest(BPD bpd, string name) =>
            bpd?.SegmentList?.Where(s => s.Name == name).OrderByDescending(s => s.Version).FirstOrDefault();

        private static List<string> Group(Data.Segment.Segment segment) =>
            segment.DataElements.Count > 3 ? Helper.SplitDataElementGroup(segment.DataElements[3].TrimEnd('\'')) : new List<string>();
    }

    internal static class InstantPayment
    {
        /// <summary>Order id and status of HIIPZ, HIIPM, HIIPS or HIISS in an answer.</summary>
        public static (string OrderId, string StatusCode) ParseAnswer(string message, string segmentId)
        {
            var segment = Helper.SplitEncryptedSegments(message ?? string.Empty).FirstOrDefault(s => s.StartsWith(segmentId + ":"));
            if (segment == null)
                return (null, null);

            var e = Helper.SplitSegmentValues(segment);
            string Text(int element) => element < e.Count && e[element].Count > 0 && e[element][0].Text.Length > 0 ? e[element][0].Text : null;
            switch (segmentId)
            {
                case "HIIPZ": return (Text(1), Text(3));
                case "HIIPM": return (Text(1), null);
                case "HIIPS": return (Text(4), Text(6));
                case "HIISS": return (Text(6), null);
                default: return (null, null);
            }
        }

        /// <summary>
        /// State from "Status SEPA-Auftrag" and the segment related return values. For a status request the return values
        /// describe the request itself (e.g. 9210 "Keine gültige IBAN des Kunden"), only the status field tells about the
        /// payment.
        /// </summary>
        public static InstantPaymentStatus Status(string statusCode, ICollection<string> codes, bool statusRequest = false)
        {
            if (codes.Contains("3270"))
                return InstantPaymentStatus.ConvertedToCreditTransfer;

            switch (statusCode)
            {
                case "7":
                    return InstantPaymentStatus.Executed;
                case "2":
                case "5":
                case "6":
                case "8":
                case "9":
                    return InstantPaymentStatus.Rejected;
                case "1":
                case "3":
                case "4":
                    return InstantPaymentStatus.Pending;
            }

            if (codes.Contains("3045") || codes.Contains("3047"))
                return InstantPaymentStatus.Pending;
            if (statusRequest)
                return InstantPaymentStatus.Unknown;
            if (codes.Any(c => c.StartsWith("9")) || codes.Contains("3042"))
                return InstantPaymentStatus.Rejected;
            if (codes.Contains("0020"))
                return InstantPaymentStatus.Executed;
            return codes.Contains("0010") ? InstantPaymentStatus.Pending : InstantPaymentStatus.Unknown;
        }
    }
}
