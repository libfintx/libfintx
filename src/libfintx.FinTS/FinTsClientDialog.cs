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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace libfintx.FinTS
{
    public partial class FinTsClient
    {
        /// <summary>
        /// Ends the dialog opened by the last business transaction (HKEND). The customer has to end every dialog
        /// (FinTS 3.0 Formals, C.4). Each business transaction opens a dialog of its own and leaves it open, so call
        /// this once the result has been processed and no TAN or approval is outstanding for it.
        /// </summary>
        /// <returns>
        /// Bank return codes; without an open dialog nothing is sent and the result has no messages.
        /// </returns>
        public async Task<HBCIDialogResult> EndDialog()
        {
            var dialogId = HNHBK;
            if (string.IsNullOrEmpty(dialogId) || dialogId == "0")
                return new HBCIDialogResult(new List<HBCIBankMessage>(), string.Empty);

            try
            {
                string bankCode = await Transaction.HKEND(this, dialogId);
                return new HBCIDialogResult(Parse_BankCode(bankCode), bankCode);
            }
            finally
            {
                // The dialog is over even if the answer reports an error or does not arrive: the bank ends it as well.
                HNHBK = null;
            }
        }
    }
}
