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
using libfintx.FinTS.Camt;
using libfintx.FinTS.Data;
using libfintx.FinTS.Message;
using libfintx.FinTS.Segments;
using System.Text;
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS
{
    public static class HKCAZ
    {
        /// <summary>
        /// Transactions in camt format: camt.052 in the version the bank offers (HICAZS), or camt.053
        /// </summary>
        public static async Task<String> Init_HKCAZ(FinTsClient client, string FromDate, string ToDate,
            string Startpoint, CamtVersion camtVers)
        {
            if (camtVers != CamtVersion.Camt052 && camtVers != CamtVersion.Camt053)
                return string.Empty;

            client.Logger.LogInformation("Starting job HKCAZ: Request transactions in {Format} format",
                camtVers == CamtVersion.Camt052 ? "camt052" : "camt053");

            string segments = Segment(client, FromDate, ToDate, Startpoint, camtVers);

            if (client.BPD.IsTANRequired("HKCAZ"))
            {
                client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg4);
                segments = HKTAN.Init_HKTAN(client, segments, "HKCAZ");
            }

            string message = FinTSMessage.Create(client, client.HNHBS, client.HNHBK, segments, client.HIRMS);
            string response = await FinTSMessage.Send(client, message);

            client.Parse_Message(response);

            return response;
        }

        /// <summary>
        /// The HKCAZ segment, without sending it. It names the account as the bank lists it in the
        /// UPD, the same one HKKAZ names: IBAN, BIC, account number, sub-account feature, country
        /// and bank code. Then the camt format, all accounts (N), the period, the maximum number
        /// of entries (empty) and the touchdown point; empty trailing elements are left out.
        /// </summary>
        internal static string Segment(FinTsClient client, string fromDate, string toDate, string startpoint, CamtVersion camtVers)
        {
            AccountInformation account = HKKAZ.ResolveAccount(client);

            client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg3);
            SEG sEG = new SEG();

            string[] elements =
            {
                string.Join(DEG.Separator, account.AccountIban, account.AccountBic, account.AccountNumber,
                    account.SubAccountFeature, SEG_COUNTRY.Germany, account.AccountBankCode),
                camtVers == CamtVersion.Camt052 ? Camt052Format(client) : CamtScheme.Camt053,
                DEG.DeAdd,
                fromDate ?? string.Empty,
                toDate ?? string.Empty,
                string.Empty,
                startpoint ?? string.Empty,
            };
            int used = elements.Length;
            while (used > 3 && elements[used - 1].Length == 0)
                used--;

            return sEG.toSEG(new SEG_DATA
            {
                Header = "HKCAZ",
                Num = client.SEGNUM,
                Version = client.HICAZS,
                RefNum = 0,
                RawData = string.Join(sEG.Delimiter, elements, 0, used) + sEG.Terminator
            });
        }

        /// <summary>
        /// The camt.052 version the bank offers in HICAZS, or camt.052.001.08 when the client has
        /// none, for example after a dialog with a stored system ID that did not fetch the BPD.
        /// </summary>
        private static string Camt052Format(FinTsClient client) =>
            string.IsNullOrWhiteSpace(client.HICAZS_Camt) ? CamtScheme.Camt052_001_08 : client.HICAZS_Camt;
    }
}
