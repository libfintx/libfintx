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
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using libfintx.FinTS.Data;
using libfintx.FinTS.Message;
using libfintx.FinTS.Segments;
using libfintx.Sepa;
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS
{
    public static class HKCCM
    {
        /// <summary>
        /// Collective transfer
        /// </summary>
        public static async Task<String> Init_HKCCM(FinTsClient client, List<Pain00100203CtData> PainData,
            string NumberofTransactions, decimal TotalAmount)
        {
            client.Logger.LogInformation("Starting job HKCCM: Collective transfer money");

            var connectionDetails = client.ConnectionDetails;
            client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg3);
            string segments = Create(client, client.SEGNUM, connectionDetails.Iban + DEG.Separator + connectionDetails.Bic,
                PainData, NumberofTransactions, TotalAmount);

            if (client.BPD.IsTANRequired("HKCCM"))
            {
                client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg4);
                segments = HKTAN.Init_HKTAN(client, segments, "HKCCM");
            }

            string message = FinTSMessage.Create(client, client.HNHBS, client.HNHBK, segments, client.HIRMS);
            var response = await FinTSMessage.Send(client, message);

            client.Parse_Message(response);

            return response;
        }

        /// <summary>
        /// HKCCM segment (SEPA-Sammelüberweisung): account, total (Summenfeld), single booking requested (empty),
        /// descriptor and pain message in the format announced in HISPAS.
        /// </summary>
        internal static string Create(FinTsClient client, int segmentNumber, string kti, List<Pain00100203CtData> painData,
            string numberOfTransactions, decimal totalAmount, string messageId = null, string debtorBic = null)
        {
            var pain = Helper.CreatePainCollectiveCreditTransfer(client, painData, numberOfTransactions, totalAmount,
                new DateTime(1999, 1, 1), messageId: messageId, debtorBic: debtorBic);
            return Helper.PaymentOrderSegment("HKCCM", 1, segmentNumber, kti,
                new[] { Converter.ToFinTsAmount(totalAmount) + DEG.Separator + "EUR", string.Empty }, pain);
        }
    }
}
