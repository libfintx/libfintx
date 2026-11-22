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

using libfintx.FinTS.Data;

namespace libfintx.FinTS
{
    /// <summary>
    /// Kontoverbindung international (KTI). It carries either IBAN and BIC or the national
    /// account (Kontonummer, Unterkontomerkmal, Kreditinstitutskennung); all elements are optional.
    /// </summary>
    public static class KTI
    {
        /// <summary>
        /// National form without IBAN and BIC: <c>::Kontonummer:Unterkontomerkmal:280:BLZ</c>.
        /// Used when an institute reports an account without IBAN in the UPD; sending an empty IBAN
        /// with a BIC instead is rejected (9010 "Das Konto besteht nicht" / "IBAN nicht vorhanden").
        /// </summary>
        public static string National(string accountNumber, string subAccountFeature, string bankCode)
        {
            return DEG.Separator + DEG.Separator + accountNumber + DEG.Separator + subAccountFeature
                + DEG.Separator + SEG_COUNTRY.Germany + DEG.Separator + bankCode;
        }

        /// <summary>
        /// Full KTI of an account: <c>IBAN:BIC:Kontonummer:Unterkontomerkmal:280:BLZ</c>, or the
        /// national form (<see cref="National"/>) when the account has no IBAN.
        /// </summary>
        public static string Of(AccountInformation account)
        {
            var national = National(account.AccountNumber, account.SubAccountFeature, account.AccountBankCode);
            if (string.IsNullOrWhiteSpace(account.AccountIban))
                return national;

            return account.AccountIban + DEG.Separator + account.AccountBic + national.Substring(DEG.Separator.Length);
        }
    }
}
