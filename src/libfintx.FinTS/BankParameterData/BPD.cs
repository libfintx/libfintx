/*	
 * 	
 *  This file is part of libfintx.
 *  
 *  Copyright (C) 2020 Abid Hussain
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

using libfintx.FinTS.Data.Segment;
using libfintx.Logger.Log;
using System;
using System.Collections.Generic;

namespace libfintx.FinTS.BankParameterData
{
    public class BPD
    {
        public string Raw { get; set; }

        public HIPINS HIPINS { get; set; }

        public List<HICAZS> HICAZS { get; set; } = new();

        public List<HIKAZS> HIKAZS { get; set; } = new();

        public List<HITANS> HITANS { get; set; } = new();

        public List<Segment> SegmentList { get; set; } = new();

        public static BPD Parse(string rawBpd)
        {
            var bpd = new BPD();
            bpd.Raw = rawBpd;
            bpd.SegmentList = new List<Segment>();
            bpd.HICAZS = new List<HICAZS>();
            bpd.HIKAZS = new List<HIKAZS>();
            bpd.HITANS = new List<HITANS>();

            var segments = Helper.SplitSegments(rawBpd);
            foreach (var rawSegment in segments)
            {
                try
                {
                    var segment = SegmentParserFactory.ParseSegment(rawSegment);
                    if (segment is HIPINS)
                        bpd.HIPINS = (HIPINS) segment;
                    else if (segment is HICAZS)
                        bpd.HICAZS.Add((HICAZS) segment);
                    else if (segment is HIKAZS)
                        bpd.HIKAZS.Add((HIKAZS) segment);
                    else if (segment is HITANS)
                        bpd.HITANS.Add((HITANS) segment);

                    bpd.SegmentList.Add(segment);
                }
                catch (Exception ex)
                {
                    Log.Write($"Couldn't parse segment: {ex.Message}{Environment.NewLine}{rawSegment}");
                }
            }

            return bpd;
        }

        /// <summary>
        /// Gives bank if TAN authentication is required for a specific FinTS message identification.
        /// </summary>
        /// <param name="gvName">
        /// The message identification code, e.g. HKCAZ, HKCCM, HKDME.
        /// </param>
        /// <returns></returns>
        public bool IsTANRequired(string gvName)
        {
            return HIPINS != null && HIPINS.IsTanRequired(gvName);
        }
    }
}
