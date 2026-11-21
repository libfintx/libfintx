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
using System.Threading.Tasks;
using libfintx.FinTS.Data;
using libfintx.FinTS.Message;
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS
{
    public static class HKSPA
    {
        /// <summary>
        /// Request SEPA account connection (SEPA-Kontoverbindung anfordern) for all accounts of the user
        /// </summary>
        public static async Task<String> Init_HKSPA(FinTsClient client)
        {
            client.Logger.LogInformation("Starting job HKSPA: Request SEPA account connection");

            client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg3);

            // No account data elements: the bank answers with the SEPA account data of all accounts of the user
            string segments = "HKSPA" + DEG.Separator + client.SEGNUM + DEG.Separator + client.HISPAS + new SEG().Terminator;

            if (client.BPD.IsTANRequired("HKSPA"))
            {
                client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg4);
                segments = HKTAN.Init_HKTAN(client, segments, "HKSPA");
            }

            string message = FinTSMessage.Create(client, client.HNHBS, client.HNHBK, segments, client.HIRMS);
            string response = await FinTSMessage.Send(client, message);

            client.Parse_Message(response);

            return response;
        }
    }
}
