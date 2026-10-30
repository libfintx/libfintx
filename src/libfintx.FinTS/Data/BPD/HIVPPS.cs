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

using System.Collections.Generic;
using libfintx.FinTS.Data.Segment;

namespace libfintx.FinTS
{
    /// <summary>
    /// Bank parameters of the verification of payee (Namensabgleich, "FinTS 3.0 Messages Geschäftsvorfälle VOP" 1.02).
    /// </summary>
    public class HIVPPS : SegmentBase
    {
        public HIVPPS(Segment segment) : base(segment)
        {
        }

        /// <summary>Maximum number of transactions of an order that are checked (0 = no limit).</summary>
        public int MaxTransactions { get; set; }

        /// <summary>The explanation text may contain formatting (&lt;br&gt;, &lt;b&gt;, &lt;ul&gt; ...).</summary>
        public bool ExplanationStructured { get; set; }

        /// <summary>Payment status reports are delivered step by step (S) or complete (V).</summary>
        public string DeliveryType { get; set; }

        /// <summary>A collective order may contain a single transaction.</summary>
        public bool CollectiveWithOneTransactionAllowed { get; set; }

        /// <summary>Supported payment status report formats (pain.002 descriptors).</summary>
        public List<string> PaymentStatusReportFormats { get; set; } = new List<string>();

        /// <summary>Orders that need the verification of payee (e.g. HKCCS, HKCCM, HKIPZ).</summary>
        public List<string> PaymentOrders { get; set; } = new List<string>();
    }
}
