/*	
 * 	
 *  This file is part of libfintx.
 *  
 *  Copyright (C) 2021 - 2022 Abid Hussain
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
using System.Text;
using libfintx.FinTS.Data.Segment;

namespace libfintx.FinTS
{
    public class HITANS : SegmentBase
    {
        public List<HITANS_TanProcess> TanProcesses { get; set; }

        public HITANS(Segment segment) : base(segment)
        {
            TanProcesses = new List<HITANS_TanProcess>();
        }
    }

    public class HITANS_TanProcess
    {
        public int TanCode { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Decoupled approval (HITANS#7, elements 22-26, FinTS 3.0 Security - PIN/TAN 2020-07-10, pp. 174 f.): maximum
        /// number of status requests (process S). 0 means no limit, <c>null</c> not given. Requesting more often may be
        /// treated by the bank like a wrong TAN (p. 111).
        /// </summary>
        public int? DecoupledMaxStatusRequests { get; set; }

        /// <summary>Decoupled approval: wait time in seconds before the first status request.</summary>
        public int? DecoupledWaitBeforeFirstStatusRequest { get; set; }

        /// <summary>Decoupled approval: wait time in seconds before each further status request.</summary>
        public int? DecoupledWaitBeforeNextStatusRequest { get; set; }

        /// <summary>Decoupled approval: the customer may confirm the approval manually.</summary>
        public bool? DecoupledManualConfirmationAllowed { get; set; }

        /// <summary>Decoupled approval: automated status requests are allowed.</summary>
        public bool? DecoupledAutomatedStatusRequestsAllowed { get; set; }
    }
}
