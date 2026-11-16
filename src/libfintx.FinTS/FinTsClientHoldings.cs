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


using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using libfintx.Swift;

namespace libfintx.FinTS
{
    public partial class FinTsClient
    {
        /// <summary>
        /// The securities statement (Depotaufstellung) of the client's account, with quantities,
        /// prices and values as the bank reports them. Fetches all parts of a long statement.
        /// Fails with a message when the bank offers no securities statement in version 5 or 6.
        /// </summary>
        public async Task<HBCIDialogResult<MT535>> Holdings(TANDialog tanDialog)
        {
            var result = await InitializeConnection();
            if (result.HasError)
                return result.TypedResult<MT535>();

            result = await ProcessSCA(result, tanDialog);
            if (result.HasError || result.IsAuthenticationPending)
                return result.TypedResult<MT535>();

            if (HIWPDS == 0)
                return new HBCIDialogResult<MT535>(
                    new[] { new HBCIBankMessage("9000", "Die Bank bietet keine Depotaufstellung (HKWPD) an.") }, null);

            var mt535 = new StringBuilder();
            string touchdown = null;
            do
            {
                string bankCode = await Transaction.HKWPD(this, touchdown);
                result = new HBCIDialogResult(Parse_BankCode(bankCode), bankCode);
                if (result.HasError)
                    return result.TypedResult<MT535>();

                result = await ProcessSCA(result, tanDialog);
                if (result.HasError || result.IsAuthenticationPending)
                    return result.TypedResult<MT535>();

                mt535.Append(Mt535Of(result.RawData));
                touchdown = result.RawData.Contains("+3040::") ? Parse_Transactions_Startpoint(result.RawData) : null;
                if (touchdown != null)
                    Parse_Message(result.RawData);
            }
            while (!string.IsNullOrEmpty(touchdown));

            return result.TypedResult(new MT535Parser(_loggerFactory).Parse(mt535.ToString()));
        }

        /// <summary>The MT535 text in the HIWPD segments of a response, read by its binary length.</summary>
        internal static string Mt535Of(string response)
        {
            var text = new StringBuilder();
            foreach (Match match in Regex.Matches(response ?? string.Empty, @"HIWPD:\d+:\d+(?::\d+)?\+@(\d+)@"))
            {
                int start = match.Index + match.Length;
                int length = int.Parse(match.Groups[1].Value);
                if (start + length <= response.Length)
                    text.Append(response, start, length).Append('\n');
            }
            return text.ToString();
        }
    }
}
