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
using System.Linq;
using libfintx.FinTS.Data;

namespace libfintx.FinTS
{
    /// <summary>
    /// Namensabgleich Prüfauftrag (verification of payee check), "FinTS 3.0 Messages Geschäftsvorfälle VOP" 1.02.
    /// </summary>
    public static class HKVPP
    {
        /// <summary>
        /// HKVPP version 1: the supported payment status report formats; when asking for further results also the
        /// polling id of HIVPP and the restart point of 3040.
        /// </summary>
        internal static string Create(int segmentNumber, HIVPPS parameters, DataElementValue pollingId = null, string restartPoint = null)
        {
            var seg = new SEG();
            var elements = new List<string>
            {
                "HKVPP" + DEG.Separator + segmentNumber + DEG.Separator + "1",
                string.Join(DEG.Separator, parameters.PaymentStatusReportFormats.Select(Helper.EscapeEdifactString)),
            };
            if (restartPoint != null)
            {
                elements.Add(pollingId?.Raw ?? string.Empty);
                elements.Add(string.Empty);
                elements.Add(Helper.EscapeEdifactString(restartPoint));
            }
            return string.Join(seg.Delimiter, elements) + seg.Terminator;
        }
    }

    /// <summary>
    /// Namensabgleich Ausführungsauftrag (verification of payee execution order), VOP 1.02.
    /// </summary>
    public static class HKVPA
    {
        /// <summary>
        /// HKVPA version 1 with the VOP id of HIVPP. The VOP id is binary (VOP 1.02, pp. 14, 27) and goes back
        /// unchanged as binary data, also when the bank sent it as text.
        /// </summary>
        internal static string Create(int segmentNumber, string vopId)
        {
            var seg = new SEG();
            return "HKVPA" + DEG.Separator + segmentNumber + DEG.Separator + "1" + seg.Delimiter + "@" + vopId.Length + "@" + vopId + seg.Terminator;
        }
    }
}
