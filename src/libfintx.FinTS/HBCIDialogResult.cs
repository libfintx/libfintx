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

using System.Collections.Generic;
using System.Linq;

namespace libfintx.FinTS
{
    public class HBCIDialogResult
    {
        public string RawData { get; internal set; }

        public bool IsSuccess => messages.Any(m => m.IsSuccess);
        public bool HasInfo => messages.Any(m => m.IsInfo);
        public bool HasWarning => messages.Any(m => m.IsWarning);
        public bool HasError => messages.Any(m => m.IsError);
        public bool HasUnknown => messages.Any(m => m.IsUnknown);

        /// <summary>
        /// Returns true if there is any message with code <i>0030</i>.
        /// </summary>
        public bool IsTanRequired => GetMessage("0030") != null;

        /// <summary>
        /// Returns true if there is any message with code <i>3955</i>.
        /// </summary>
        public bool IsApprovalRequired => GetMessage("3955") != null;

        public bool IsSCARequired => IsTanRequired || IsApprovalRequired;

        /// <summary>
        /// Returns true if there is any message with code <i>3955</i>.
        /// </summary>
        public bool IsWaitingForApproval => GetMessage("3956") != null;

        private readonly List<HBCIBankMessage> messages;
        public IEnumerable<HBCIBankMessage> Messages => messages;

        public HBCIDialogResult(IEnumerable<HBCIBankMessage> messages, string rawData)
        {
            this.messages = messages.ToList();
            RawData = rawData;
        }

        public string GetMessage(string code)
        {
            return messages.FirstOrDefault(m => m.Code == code)?.Message;
        }

        /// <summary>
        /// Segment numbers of the sent message whose return values do not describe the payment order itself, e.g. the
        /// verification of payee (HKVPP/HKVPA answer "0020 Auftrag ausgeführt" for the check, not for the payment).
        /// </summary>
        internal HashSet<int> IgnoredSegmentReferences { get; } = new HashSet<int>();

        /// <summary>
        /// Ignored segments whose errors make the outcome unclear rather than "not executed": HKVPA answers 9210 "Auftrag
        /// existiert nicht bzw. wurde bereits ausgeführt" (VOP 1.02, p. 15). Errors about HKVPP (e.g. 9250 "submit later")
        /// still mean "not executed".
        /// </summary>
        internal HashSet<int> UnclearOnErrorSegmentReferences { get; } = new HashSet<int>();

        /// <summary>
        /// Outcome of the payment order answered by this result, taken only from the segment related return values
        /// (HIRMS); "0010" in HIRMG only means "message received". Contradictory answers (success and error at once,
        /// 9050 "partly erroneous") are <see cref="OrderStatus.Unclear"/>, never <see cref="OrderStatus.NotExecuted"/>:
        /// resubmitting could pay twice.
        /// </summary>
        /// <param name="instantPayment">For instant payments the bank accepts with 3045/3047/3270 instead of 0010/0020.</param>
        public OrderStatus GetOrderStatus(bool instantPayment = false)
        {
            var relevant = messages.Where(m => !(m.SegmentReference is int s && IgnoredSegmentReferences.Contains(s)
                && (!m.IsError || UnclearOnErrorSegmentReferences.Contains(s)))).ToList();
            // an error about HKVPA alone may mean "order does not exist or was already executed" (VOP 1.02, p. 15)
            var ignoredError = messages.Any(m => m.IsError && m.SegmentReference is int r && UnclearOnErrorSegmentReferences.Contains(r));
            var order = new HashSet<string>(relevant.Where(m => m.SegmentReference != null).Select(m => m.Code));
            var dialog = new HashSet<string>(relevant.Where(m => m.SegmentReference == null).Select(m => m.Code));
            bool Has(params string[] codes) => codes.Any(order.Contains);

            var success = Has("0010", "0020") || (instantPayment && Has("3045", "3047", "3270"));
            var error = order.Any(c => c.StartsWith("9") && c != "9220") || dialog.Any(c => c.StartsWith("9") && c != "9050");

            if (Has("3210", "3220") || (Has("9220") && success))
                return OrderStatus.PartiallyAccepted;
            if (Has("9220") || (error && success))
                return OrderStatus.Unclear;
            if (error || Has("3042"))
                return OrderStatus.NotExecuted;
            if (ignoredError)
                return OrderStatus.Unclear;
            if (Has("3955", "3956") || dialog.Contains("3955") || dialog.Contains("3956"))
                return OrderStatus.Unclear;
            if (Has("0030", "3945"))
                return OrderStatus.NotExecuted;
            if (success && !dialog.Contains("9050"))
                return OrderStatus.Accepted;
            return OrderStatus.Unclear;
        }

        /// <summary>
        /// Return values about single items of a collective order that were not executed (3210, 3220, 9220).
        /// </summary>
        public IEnumerable<HBCIBankMessage> FailedItems => messages.Where(m => m.Code == "3210" || m.Code == "3220" || m.Code == "9220");

        public HBCIDialogResult<T> TypedResult<T>()
        {
            var result = new HBCIDialogResult<T>(Messages, RawData);
            result.IgnoredSegmentReferences.UnionWith(IgnoredSegmentReferences);
            result.UnclearOnErrorSegmentReferences.UnionWith(UnclearOnErrorSegmentReferences);
            return result;
        }

        public HBCIDialogResult<T> TypedResult<T>(T data)
        {
            var result = new HBCIDialogResult<T>(Messages, RawData, data);
            result.IgnoredSegmentReferences.UnionWith(IgnoredSegmentReferences);
            result.UnclearOnErrorSegmentReferences.UnionWith(UnclearOnErrorSegmentReferences);
            return result;
        }

        public override string ToString()
        {
            return string.Join(", ", messages);
        }
    }

    public class HBCIDialogResult<T> : HBCIDialogResult
    {
        public T Data { get; set; }
        public HBCIDialogResult(IEnumerable<HBCIBankMessage> bankMessages, string rawData) : base(bankMessages, rawData) { }
        public HBCIDialogResult(IEnumerable<HBCIBankMessage> bankMessages, string rawData, T data) : base(bankMessages, rawData)
        {
            Data = data;
        }
    }

    public class HBCIBankMessage
    {
        public enum TypeEnum
        {
            Success, Info, Warning, Error, Unknown
        }
        public TypeEnum Type { get; }
        public bool IsSuccess { get => TypeEnum.Success == Type; }
        public bool IsInfo { get => TypeEnum.Info == Type; }
        public bool IsWarning { get => TypeEnum.Warning == Type; }
        public bool IsError { get => TypeEnum.Error == Type; }
        public bool IsUnknown { get => TypeEnum.Error == Type; }
        public string Code { get; }
        public string Message { get; }

        /// <summary>
        /// Number of the segment of the sent message this return value refers to (HIRMS); <c>null</c> for return values
        /// about the whole message (HIRMG).
        /// </summary>
        public int? SegmentReference { get; }

        /// <summary>Parameters of the return value, e.g. the restart point of 3040.</summary>
        public IReadOnlyList<string> Parameters { get; }

        public HBCIBankMessage(string code, string message) : this(code, message, null, null)
        {
        }

        public HBCIBankMessage(string code, string message, int? segmentReference, IReadOnlyList<string> parameters)
        {
            Code = code;
            Message = message;
            SegmentReference = segmentReference;
            Parameters = parameters ?? new List<string>();
            if (code.StartsWith("0"))
                Type = TypeEnum.Success;
            else if (code.StartsWith("1"))
                Type = TypeEnum.Info;
            else if (code.StartsWith("3"))
                Type = TypeEnum.Warning;
            else if (code.StartsWith("9"))
                Type = TypeEnum.Error;
            else
                Type = TypeEnum.Unknown;
        }

        public override string ToString()
        {
            return $"{Code}: {Message}";
        }
    }
}
