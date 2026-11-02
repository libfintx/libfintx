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
using System.Text.RegularExpressions;

namespace libfintx.FinTS.Data.Segment
{
    internal class HITANSSegmentParser : ISegmentParser
    {
        public Segment ParseSegment(Segment segment)
        {
            var result = new HITANS(segment);

            // HITANS:175:7:4+1+1+1+N:N:0:922:2:pushTAN-dec:Decoupled::pushTAN 2.0:::Aufforderung:2048:J:2:N:0:0:N:N:00:2:N:2:180:1:1:J:J'
            var matches = Regex.Matches(segment.Payload, @"(?<1>9\d{2}):(?<2>1|2):(?<3>.+?):(?<4>.*?):(?<5>.*?):(?<6>.+?):");
            if (matches.Count == 0)
                throw new ArgumentException($"Could not parse segment '{segment.Name}':{Environment.NewLine}{segment.Payload}");

            foreach (Match match in matches)
            {
                var tanProcess = new HITANS_TanProcess();

                var tanCode = match.Groups["1"].Value;
                if (tanCode != null)
                    tanProcess.TanCode = Convert.ToInt32(tanCode);

                var processName = match.Groups["6"].Value;
                if (processName != null)
                    tanProcess.Name = processName;

                result.TanProcesses.Add(tanProcess);
            }

            if (segment.Version >= 7 && segment.DataElements.Count >= 4)
                ParseDecoupledParameters(segment.DataElements[3], result.TanProcesses);

            return result;
        }

        /// <summary>
        /// Reads elements 22-26 of each TAN process from the parameter group of HITANS#7: three leading elements, then
        /// 26 elements per process (FinTS 3.0 Security - PIN/TAN 2020-07-10, pp. 173-175). Optional elements at the end
        /// of the group may be omitted by the bank.
        /// </summary>
        private static void ParseDecoupledParameters(string parameters, List<HITANS_TanProcess> tanProcesses)
        {
            if (parameters.EndsWith("'") && !parameters.EndsWith("?'"))
                parameters = parameters.Substring(0, parameters.Length - 1);

            var elements = Helper.SplitDataElementGroup(parameters);
            string At(int index) => index < elements.Count ? elements[index] : string.Empty;

            for (int i = 3; i < elements.Count; i += 26)
            {
                if (!int.TryParse(elements[i], out var tanCode))
                    continue;

                var tanProcess = tanProcesses.Find(p => p.TanCode == tanCode);
                if (tanProcess == null)
                    continue;

                tanProcess.DecoupledMaxStatusRequests = ParseNumber(At(i + 21));
                tanProcess.DecoupledWaitBeforeFirstStatusRequest = ParseNumber(At(i + 22));
                tanProcess.DecoupledWaitBeforeNextStatusRequest = ParseNumber(At(i + 23));
                tanProcess.DecoupledManualConfirmationAllowed = ParseYesNo(At(i + 24));
                tanProcess.DecoupledAutomatedStatusRequestsAllowed = ParseYesNo(At(i + 25));
            }
        }

        private static int? ParseNumber(string value) => int.TryParse(value, out var number) ? number : null;

        private static bool? ParseYesNo(string value) => value == "J" ? true : value == "N" ? false : null;

    }
}
