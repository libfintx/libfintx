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

namespace libfintx.Swift;

/// <summary>
/// A SWIFT statement, either from a MT940 or MT942 message.
/// </summary>
public class SwiftStatement
{
    /// <summary>
    /// Transaction Reference Number
    ///
    /// This field specifies the reference assigned by the Sender to unambiguously identify the message.
    ///
    /// Field 20.
    /// 
    /// See also: https://www2.swift.com/knowledgecentre/publications/us9m_20230720/2.0?topic=con_sfld_MaOrQQQQEe2AI4OK6vBjrg_1537937705fld.htm
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// This field contains the sequential number of the statement, optionally followed by the sequence
    /// number of the message within that statement when more than one message is sent for the statement.
    ///
    /// Field 28C.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// BIC
    ///
    /// Field 25a part 1
    /// </summary>
    public string BankCode { get; set; }

    /// <summary>
    /// Account number
    ///
    /// Field 25a part 2
    /// </summary>
    public string AccountCode { get; set; }

    /// <summary>
    /// The currency of the statement in ISO 4217 currency code.
    ///
    /// Internally this is taken from the opening balance.
    ///
    /// MT940 field 60a or MT942 field 34F.
    /// </summary>
    public string Currency { get; set; }

    #region MT 940 - 5. Field 60a: Opening Balance

    /// <summary>
    /// This field specifies the (intermediate) opening balance amount.
    ///
    /// When positive, it is in Debit, if negative, it is in credit.
    ///
    /// MT940 field 60.
    /// </summary>
    public decimal StartBalance { get; set; }

    /// <summary>
    /// This field specifies the (intermediate) opening balance date.
    /// </summary>
    public DateTime StartDate { get; set; }

    #endregion

    #region MT 940 - 8. Field 62a: Closing Balance (Booked Funds)

    /// <summary>
    /// This field specifies the (intermediate) closing balance.
    ///
    /// When positive, it is in Debit, if negative, it is in credit.
    ///
    /// MT940 field 62a.
    /// </summary>
    public decimal EndBalance { get; set; }

    /// <summary>
    /// This field specifies the (intermediate) closing balance date.
    /// </summary>
    public DateTime EndDate { get; set; }

    #endregion

    #region MT942 specific fields

    public bool Pending { get; set; }

    #region MT942 - Field 34F

    /// <summary>
    /// This field specifies the minimum value (transaction amount) reported in the message, Debit.
    ///
    /// Field 34F.
    ///
    /// See also <a href="https://www2.swift.com/knowledgecentre/publications/us9m_20230720/2.0?topic=con_sfld_MaOo4QQQEe2AI4OK6vBjrg_-1373286813fld.htm">MT 942 - 5. Field 34F: Debit/(Debit and Credit) Floor Limit Indicator</a>.
    /// </summary>
    public decimal SmallestAmount { get; set; }

    /// <summary>
    /// This field specifies the minimum value (transaction amount) reported in the message, Credit.
    ///
    /// Field 34F.
    ///
    /// See also <a href="https://www2.swift.com/knowledgecentre/publications/us9m_20230720/2.0?topic=con_sfld_MaOo4QQQEe2AI4OK6vBjrg_-1373286813fld.htm">MT 942 - 5. Field 34F: Debit/(Debit and Credit) Floor Limit Indicator</a>.
    /// </summary>
    public decimal SmallestCreditAmount { get; set; }

    #endregion

    /// <summary>
    /// Date/Time Indication
    ///
    /// This field indicates the date, time and time zone at which the report was created.
    ///
    /// Field 13D.
    ///
    /// See also <a href="https://www2.swift.com/knowledgecentre/publications/us9m_20230720/2.0?topic=con_sfld_MaOpCQQQEe2AI4OK6vBjrg_6025205fld.htm">MT 942 - 7. Field 13D: Date/Time Indication</a>.
    /// </summary>
    public DateTime CreationDate { get; set; }

    #region MT942 - Field 90D

    /// <summary>
    /// This field indicates the total number of debit entries.
    ///
    /// MT942 field 90D.
    ///
    /// See also <a href="https://www2.swift.com/knowledgecentre/publications/us9m_20230720/2.0?topic=con_sfld_MaOpSwQQEe2AI4OK6vBjrg_1118897675fld.htm">MT 942 - 10. Field 90D: Number and Sum of Entries</a>.
    /// </summary>
    public int CountDebit { get; set; }

    /// <summary>
    /// This field indicates the total amount of debit entries.
    ///
    /// MT942 field 90D.
    ///
    /// See also <a href="https://www2.swift.com/knowledgecentre/publications/us9m_20230720/2.0?topic=con_sfld_MaOpSwQQEe2AI4OK6vBjrg_1118897675fld.htm">MT 942 - 10. Field 90D: Number and Sum of Entries</a>.
    /// </summary>
    public decimal AmountDebit { get; set; }

    #endregion

    #region MT942 - Field 90C

    /// <summary>
    /// This field indicates the total number of credit entries.
    ///
    /// MT942 field 90C.
    ///
    /// See also <a href="https://www2.swift.com/knowledgecentre/publications/us9m_20230720/2.0?topic=con_sfld_MaOpWwQQEe2AI4OK6vBjrg_1425575559fld.htm">MT 942 - 11. Field 90C: Number and Sum of Entries</a>.
    /// </summary>
    public int CountCredit { get; set; }

    /// <summary>
    /// This field indicates the total amount of credit entries.
    ///
    /// MT942 field 90C.
    ///
    /// See also <a href="https://www2.swift.com/knowledgecentre/publications/us9m_20230720/2.0?topic=con_sfld_MaOpWwQQEe2AI4OK6vBjrg_1425575559fld.htm">MT 942 - 11. Field 90C: Number and Sum of Entries</a>.
    /// </summary>
    public decimal AmountCredit { get; set; }

    #endregion

    #endregion // MT942 specific fields

    /// <summary>
    /// The SWIFT transactions in this statement.
    ///
    /// Field 61 (Statement Line) and field 86 (Information to Account Owner).
    /// </summary>
    public List<SwiftTransaction> SwiftTransactions { get; set; } = new();

    /// <summary>
    /// The raw SWIFT lines.
    /// </summary>
    public List<SwiftLine> Lines { get; set; } = new();
}
