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
using System.Linq;

namespace libfintx.FinTS.Data.Segment
{
    internal class HIVPPSSegmentParser : ISegmentParser
    {
        public Segment ParseSegment(Segment segment)
        {
            var result = new HIVPPS(segment);

            // HIVPPS:40:1:4+1+1+0+0:J:V:N:N:urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.002.001.10:HKCCS:HKIPZ'
            // 1 max. transactions, 2 explanation structured, 3 delivery type, 4 collective with one transaction allowed,
            // 5 entering the number of entries allowed, 6 supported payment status report formats (";"), 7.. orders
            if (segment.DataElements.Count < 4)
                throw new ArgumentException($"Could not parse segment '{segment.Name}':{Environment.NewLine}{segment.Payload}");

            var p = Helper.SplitDataElementGroup(segment.DataElements[3].TrimEnd('\''));
            string At(int i) => i < p.Count ? p[i] : string.Empty;

            result.MaxTransactions = int.TryParse(At(0), out var max) ? max : 0;
            result.ExplanationStructured = At(1) == "J";
            result.DeliveryType = At(2);
            result.CollectiveWithOneTransactionAllowed = At(3) == "J";
            result.PaymentStatusReportFormats = At(5).Split(';').Select(f => f.Trim()).Where(f => f.Length > 0).ToList();
            result.PaymentOrders = p.Skip(6).Select(o => o.Trim().ToUpperInvariant()).Where(o => o.Length > 0).ToList();

            return result;
        }
    }
}
