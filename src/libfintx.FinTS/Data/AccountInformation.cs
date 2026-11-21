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

using libfintx.FinTS.Util;
using System.Collections.Generic;
using System.Linq;

namespace libfintx.FinTS
{
    public class AccountInformation
    {
        public string AccountIban { get; set; }
        public string SubAccountFeature { get; set; }
        public string AccountNumber { get; set; }
        public string AccountBankCode { get; set; }
        public string AccountUserId { get; set; }
        public string AccountOwner { get; set; }
        /// <summary>The bank's name for the account product, e.g. "Sparkassenbuch Gold".</summary>
        public string AccountType { get; set; }
        /// <summary>
        /// The FinTS account kind (Kontoart, 1-99) the bank reports in the UPD, or null when it
        /// sends none. The ranges name the kind: 1-9 current account, 10-19 savings,
        /// 20-29 fixed deposit, 30-39 securities account, 40-49 loan, 50-59 credit card,
        /// 60-69 fund account, 70-79 building savings, 80-89 insurance, 90-99 other.
        /// </summary>
        public int? AccountKind { get; set; }
        public string AccountCurrency { get; set; }
        public string AccountBic { get; set; }

        public List<AccountPermission> AccountPermissions { get; set; }

        public bool IsSegmentPermitted(string segment)
        {
            return AccountPermissions.Any(a => a.Segment == segment);
        }

        public override string ToString()
        {
            return ReflectionUtil.ToString(this);
        }
    }
}
