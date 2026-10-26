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
using libfintx.FinTS.Segments;
using libfintx.Sepa;
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS
{
    public static class HKCCS
    {
        /// <summary>
        /// Transfer
        /// </summary>
        public static async Task<String> Init_HKCCS(FinTsClient client, string ReceiverName, string ReceiverIBAN,
            string ReceiverBIC, decimal Amount, string Usage)
        {
            client.Logger.LogInformation("Starting job HKCCS: Transfer money");

            var connectionDetails = client.ConnectionDetails;
            client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg3);
            string segments = Create(client, client.SEGNUM, connectionDetails.Iban + DEG.Separator + connectionDetails.Bic,
                ReceiverName, ReceiverIBAN, ReceiverBIC, Amount, Usage);

            if (client.BPD.IsTANRequired("HKCCS"))
            {
                client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg4);
                segments = HKTAN.Init_HKTAN(client, segments, "HKCCS");
            }

            string message = FinTSMessage.Create(client, client.HNHBS, client.HNHBK, segments, client.HIRMS);
            var response = await FinTSMessage.Send(client, message);

            client.Parse_Message(response);

            return response;
        }

        /// <summary>
        /// HKCCS segment (SEPA-Einzelüberweisung) with the pain message in the format announced in HISPAS.
        /// <paramref name="kti"/> is the account of the client (Kontoverbindung international), <paramref name="messageId"/>
        /// the pain message id; the end to end id of the transfer is "messageId-1".
        /// </summary>
        internal static string Create(FinTsClient client, int segmentNumber, string kti, string receiverName,
            string receiverIBAN, string receiverBIC, decimal amount, string usage, string messageId = null)
        {
            var pain = Helper.CreatePainCreditTransfer(client, receiverName, receiverIBAN, receiverBIC, amount, usage,
                new DateTime(1999, 1, 1), messageId: messageId);
            return Helper.PaymentOrderSegment("HKCCS", 1, segmentNumber, kti, null, pain);
        }
    }
}
