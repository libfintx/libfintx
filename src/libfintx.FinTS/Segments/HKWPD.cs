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
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS
{
    public static class HKWPD
    {
        /// <summary>
        /// Securities statement (Depotaufstellung). Versions 5 and 6 carry the securities account
        /// the same way (account number, sub-account feature, country, bank code); currency,
        /// price quality and the number of entries are left to the bank. A touchdown point asks
        /// for the next part of a long statement.
        /// </summary>
        public static async Task<string> Init_HKWPD(FinTsClient client, string touchdownPoint)
        {
            client.Logger.LogInformation("Starting job HKWPD: Request securities statement");

            string segments = Segment(client, touchdownPoint);

            if (client.BPD.IsTANRequired("HKWPD"))
            {
                client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg4);
                segments = HKTAN.Init_HKTAN(client, segments, "HKWPD");
            }

            string message = FinTSMessage.Create(client, client.HNHBS, client.HNHBK, segments, client.HIRMS);
            string response = await FinTSMessage.Send(client, message);

            client.Parse_Message(response);

            return response;
        }

        /// <summary>The HKWPD segment for the client's account, without sending it.</summary>
        internal static string Segment(FinTsClient client, string touchdownPoint)
        {
            var connectionDetails = client.ConnectionDetails;
            AccountInformation account = client.activeAccount ?? new AccountInformation
            {
                AccountNumber = connectionDetails.Account,
                AccountBankCode = connectionDetails.Blz.ToString(),
                SubAccountFeature = connectionDetails.SubAccount,
            };

            client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg3);
            SEG sEG = new SEG();

            StringBuilder sb = new StringBuilder();
            sb.Append(account.AccountNumber);
            sb.Append(DEG.Separator);
            sb.Append(account.SubAccountFeature);
            sb.Append(DEG.Separator);
            sb.Append(SEG_COUNTRY.Germany);
            sb.Append(DEG.Separator);
            sb.Append(account.AccountBankCode);
            if (!string.IsNullOrEmpty(touchdownPoint))
            {
                // Currency, price quality and maximum number of entries stay empty.
                sb.Append(sEG.Delimiter);
                sb.Append(sEG.Delimiter);
                sb.Append(sEG.Delimiter);
                sb.Append(sEG.Delimiter);
                sb.Append(touchdownPoint);
            }
            sb.Append(sEG.Terminator);

            return sEG.toSEG(new SEG_DATA
            {
                Header = "HKWPD",
                Num = client.SEGNUM,
                Version = client.HIWPDS,
                RefNum = 0,
                RawData = sb.ToString()
            });
        }
    }
}
