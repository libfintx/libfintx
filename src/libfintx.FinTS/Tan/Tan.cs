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

using System;
using System.Text;
using System.Threading.Tasks;
using libfintx.FinTS.Data;
using libfintx.FinTS.Message;
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS
{
    // HKTAN#7 decoupled S implementieren
    public static class Tan
    {
        /// <summary>
        /// TAN (process 2) or the first status request of a decoupled approval (process S) sent together with
        /// <paramref name="extraSegments"/> in one message, e.g. HKVPA (VOP 1.02, figure 3: "HKVPA + HKTAN"). HKTAN
        /// version 6/7: element 6 order reference, element 7 "Weitere TAN folgt" (PIN/TAN 2020-07-10, pp. 59, 65).
        /// </summary>
        /// <param name="extraSegments">Builds the segments from the first segment number; returns them with the number
        /// of the last one.</param>
        internal static async Task<String> Send_TAN(FinTsClient client, string TAN, Func<int, (string Segments, int LastSegmentNumber)> extraSegments)
        {
            if (client.HITANS < 6)
                throw new NotSupportedException($"Segments together with the TAN need HKTAN version 6 or 7 (HKTAN version {client.HITANS}).");

            client.Logger.LogInformation("Starting TAN process with additional segments");

            var (segments, last) = extraSegments(Convert.ToInt16(SEG_NUM.Seg3));
            SEG sEG = new SEG();
            client.SEGNUM = last + 1;
            segments += "HKTAN" + DEG.Separator + client.SEGNUM + DEG.Separator + client.HITANS
                + sEG.Delimiter + (TAN != null ? "2" : "S")
                + sEG.Delimiter + sEG.Delimiter + sEG.Delimiter + sEG.Delimiter + client.HITAN
                + sEG.Delimiter + DEG.DeAdd + sEG.Terminator;

            string message = FinTSMessage.Create(client, client.HNHBS, client.HNHBK, segments, client.HIRMS + (TAN != null ? ":" + TAN : null));
            string response = await FinTSMessage.Send(client, message);

            client.Parse_Message(response);

            return response;
        }

        /// <summary>
        /// TAN
        /// </summary>
        public static async Task<String> Send_TAN(FinTsClient client, string TAN)
        {
            client.Logger.LogInformation("Starting TAN process");
            string segments = string.Empty;

            StringBuilder sb = new StringBuilder();
            SEG sEG = new SEG();

            if (string.IsNullOrEmpty(client.HITAB)) // TAN Medium Name not set
            {
                // Version 2
                if (client.HITANS == 2)
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+2++" + client.HITAN + "++N'";
                }
                    
                // Version 3
                else if (client.HITANS == 3)
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+2++" + client.HITAN + "++N'";
                }

                // Version 4
                else if (client.HITANS == 4)
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+2++" + client.HITAN + "++N'";
                }

                // Version 5
                else if (client.HITANS == 5)
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+2++++" + client.HITAN + "++N'";
                }

                // Version 6
                else if (client.HITANS == 6)
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    // Element 7 "Weitere TAN folgt" follows the order reference (element 6) directly
                    sb.Append(DEG.DeAdd);
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+2++++" + client.HITAN + "+N'";
                }

                // Version 7 -> decoupled
                // FinTS_3.0_Security_Sicherheitsverfahren_PINTAN_2020-07-10_final_version.pdf Seite 64 - 65
                else if (client.HITANS == 7)
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    // Process 2 submits a TAN; process S is the status request of a decoupled approval
                    sb.Append(TAN != null ? "2" : "S");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+S++++" + client.HITAN + "+N'";
                }

                else // default
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    sb.Append("S");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+2++++" + client.HITAN + "++N'";
                }

            }
            else
            {
                // Version 2
                if (client.HITANS == 2)
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(Helper.EscapeEdifactString(client.HITAB));
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+2++" + client.HITAN + "++N++++" + client.HITAB + "'";

                }
                // Version 3
                else if (client.HITANS == 3)
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(Helper.EscapeEdifactString(client.HITAB));
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+2++" + client.HITAN + "++N++++" + client.HITAB + "'";
                }

                // Version 4
                else if (client.HITANS == 4)
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(Helper.EscapeEdifactString(client.HITAB));
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+2++" + client.HITAN + "++N++++" + client.HITAB + "'";
                }

                // Version 5
                else if (client.HITANS == 5)
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(Helper.EscapeEdifactString(client.HITAB));
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+2++++" + client.HITAN + "++N++++" + client.HITAB + "'";
                }

                // Version 6
                else if (client.HITANS == 6)
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    // The TAN medium (element 12) is optional for process 2 and S and is not sent
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+2++++" + client.HITAN + "+N++++" + client.HITAB + "'";
                }

                // Version 7 -> decoupled
                // FinTS_3.0_Security_Sicherheitsverfahren_PINTAN_2020-07-10_final_version.pdf Seite 64 - 65
                else if (client.HITANS == 7)
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    // Process 2 submits a TAN; process S is the status request of a decoupled approval
                    sb.Append(TAN != null ? "2" : "S");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    // The TAN medium (element 12) is optional for process 2 and S and is not sent
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+S++++" + client.HITAN + "+N++++" + client.HITAB + "'";
                }

                else // default
                {
                    sb.Append("HKTAN");
                    sb.Append(DEG.Separator);
                    sb.Append(SEG_NUM.Seg3);
                    sb.Append(DEG.Separator);
                    sb.Append(client.HITANS);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(client.HITAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(DEG.DeAdd);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(Helper.EscapeEdifactString(client.HITAB));
                    sb.Append(sEG.Terminator);
                    segments = sb.ToString();
                    // segments = "HKTAN:" + SEG_NUM.Seg3 + ":" + client.HITANS + "+2++" + client.HITAN + "++N++++" + client.HITAB + "'";
                }

            }

            client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg3);

            string message = FinTSMessage.Create(client, client.HNHBS, client.HNHBK, segments, client.HIRMS + (TAN != null ? ":" + TAN : null));
            string response = await FinTSMessage.Send(client, message);

            client.Parse_Message(response);

            return response;
        }
    }
}
