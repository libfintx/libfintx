﻿/*	
* 	
*  This file is part of libfintx.
*  
*  Copyright (C) 2018 Bjoern Kuensting
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
*  Updates done by Torsten Klement <torsten.klinger@googlemail.com>
*  
*  Updates Copyright (c) 2024 Torsten Klement
* 	
*/

namespace libfintx.EBICS.Parameters
{
    public class DirectDebitTransactionInfo
    {
        public string DebtorName { get; set; }
        public string DebtorAccount { get; set; }
        public string DebtorAgent { get; set; }
        public string Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string RemittanceInfo { get; set; }
        public string EndToEndId { get; set; }
        public string MandateId { get; set; }
        public string MandateSignatureDate { get; set; }
        public string SequenceType { get; set; }
    }
}
