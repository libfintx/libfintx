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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace libfintx.FinTS.Data.Segment
{
    internal class HISPASSegmentParser : ISegmentParser
    {
        public Segment ParseSegment(Segment segment)
        {
            var result = new HISPAS(segment);

            // HISPAS:147:1:4+1+1+0+J:N:N:urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.001.03:urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.008.001.02
            var match = Regex.Match(segment.Payload, @"(?<2>\d{1,3})\+(?<3>\d)\+(?<4>\d)\+(?<5>.+)");
            if (!match.Success)
                throw new ArgumentException($"Could not parse segment '{segment.Name}':{Environment.NewLine}{segment.Payload}");

            var paramSepaAccount = match.Groups["5"].Value;
            match = Regex.Match(paramSepaAccount, @"(?<1>J|N):(?<2>J|N)");
            if (!match.Success)
                throw new ArgumentException($"Could not parse SEPA account info in segment '{segment.Name}':{Environment.NewLine}{paramSepaAccount}");

            result.IsAccountNationalAllowed = "J".Equals(match.Groups["2"].Value);

            // Version 3: 1 single account retrieval, 2 national account allowed, 3 structured remittance information,
            // 4 entering the number of entries, 5 reserved remittance information positions, 6.. supported formats
            var elements = Helper.SplitDataElementGroup(paramSepaAccount.TrimEnd('\''));
            result.PainFormats = elements.Where(e => e.Contains("pain.")).ToList();
            if (segment.Version >= 3 && elements.Count > 4 && int.TryParse(elements[4], out var reserved))
                result.ReservedPurposeLength = reserved;

            return result;
        }
    }
}
