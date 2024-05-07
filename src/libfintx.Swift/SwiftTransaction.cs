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
using System.ComponentModel.DataAnnotations;

namespace libfintx.Swift;

/// <summary>
/// A single SWIFT transaction.
///
/// In MT940 messages, this is the statement line (field 61) and the information to the account owner (field 86) according to the german standard.
/// See also <a href="https://www2.swift.com/knowledgecentre/publications/us9m_20230720/2.0?topic=con_sfld_MaOrowQQEe2AI4OK6vBjrg_1576699121fld.htm">MT 940 - 6. Field 61: Statement Line</a> and
/// <a href="https://www2.swift.com/knowledgecentre/publications/us9m_20230720/2.0?topic=con_sfld_MaOruwQQEe2AI4OK6vBjrg_-110767701fld.htm">MT 940 - 7. Field 86: Information to Account Owner</a>.
/// </summary>
public class SwiftTransaction
{
    #region Field 61: Statement Line

    /// <summary>
    /// Value Date, is the date on which the debit/credit is effective.
    ///
    /// Represents in message MT940 field 61 subfield 1.
    /// </summary>
    public DateTime ValueDate { get; set; }

    /// <summary>
    /// Entry Date, is the date on which the transaction is booked to the account.
    ///
    /// Optional.
    ///
    /// Represents in message MT940 field 61 subfield 2.
    /// </summary>
    public DateTime? EntryDate { get; set; }

    /// <summary>
    /// Obsolete. Is equal to <see cref="EntryDate"/>.
    ///
    /// Represents in message MT940 field 61 subfield 2.
    /// </summary>
    [Obsolete($"Please use {nameof(EntryDate)} instead")]
    public DateTime InputDate
    {
        get => EntryDate ?? default;
        set => EntryDate = value;
    }

    /// <summary>
    /// The amount of the transaction.
    ///
    /// Positive amounts represent a Debit transaction, negative amounts represent a Credit transaction.
    ///
    /// Reverse transactions will reverse the amount.
    ///
    /// Represents in message MT940 field 61 subfield 3 and 5.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction Type and Identification Code, see description in the Codes section.
    ///
    /// Represents in message MT940 field 61 subfield 6.
    ///
    /// See also <a href="https://www2.swift.com/knowledgecentre/publications/us9m_20230720/2.0?topic=con_sfld_MaOrowQQEe2AI4OK6vBjrg_1576699121fld.htm">MT 940 - 6. Field 61: Statement Line</a> or
    /// <a href="https://www.hettwer-beratung.de/business-portfolio/zahlungsverkehr/elektr-kontoinformationen-swift-mt-940/">Fachwissen Zahlungsverkehr - Elektronische Kontoinformationen via SWIFT Format MT 940</a> (in german).
    /// </summary>
    public string TransactionTypeId { get; set; }

    /// <summary>
    /// Reference for the Account Owner (Customer), is the reference of the message (SWIFT or any other)
    /// or document that resulted in this entry. This is a reference that the account owner can
    /// use to identify the reason for the entry.
    ///
    /// Represents in message MT940 field 61 subfield 7.
    /// </summary>
    public string CustomerReference { get; set; }

    /// <summary>
    /// Reference of the Account Servicing Institution (Bank), is the reference of the advice or instruction
    /// sent by the account servicing institution to the account owner.
    ///
    /// Represents in message MT940 field 61 subfield 8.
    /// </summary>
    public string BankReference { get; set; }

    /// <summary>
    /// Supplementary Details.
    ///
    /// Represents in message MT940 field 61 subfield 9.
    /// 
    /// The following rules apply to this field:
    /// <ul>
    ///   <li>When no reference for the account owner (customer) is available, that is, subfield 7, Reference for the Account Owner, contains NONREF, the account servicing institution (bank) should provide the best available alternative information in this subfield.</li>
    ///   <li>Supplementary details may be provided when an advice has not been sent for a transaction, or to provide additional information to facilitate reconciliation.</li>
    ///   <li>This field may contain ERI to transport dual currencies, as explained in the chapter "Euro-Related Information (ERI)" in the Standards MT General Information.</li>
    ///   <li>In order to comply with the EC-directive on cross border credit transfers, the optional code EXCH may be used to transport an exchange rate. In line with ERI, the code EXCH is placed between slashes, followed by the exchange rate, format 12d, and terminated with another slash.</li>
    /// </ul>
    /// </summary>
    public string OtherInformation { get; set; }

    #endregion

    #region Field 86: Information to Account Owner

    // Field 86 is individual per country. See also https://www2.swift.com/knowledgecentre/publications/us9m_20230720/2.0?topic=con_sfld_MaOsAwQQEe2AI4OK6vBjrg_-647084768fld.htm.
    //
    // The following properties/fields are filled according to
    // the definition of the german organisation "Deutsche Kreditwirtschaft".
    // See also: https://www.hettwer-beratung.de/business-portfolio/zahlungsverkehr/elektr-kontoinformationen-swift-mt-940/

    /// <summary>
    /// Buchungstext (Erläuterung GVC, z.B. Gutschrift oder Lastschrift)
    ///
    /// Feldschlüssel: 00
    /// </summary>
    [StringLength(27)]
    public string? Text { get; set; }

    /// <summary>
    /// Primanoten Nr.
    ///
    /// Feldschlüssel: 10
    /// </summary>
    [StringLength(10)]
    public string? Primanota { get; set; }

    /// <summary>
    /// Geschaeftsvorfallcode
    /// </summary>
    public string TypeCode { get; set; }

    /// <summary>
    /// Verwendungszweck, Empfängername
    ///
    /// Feldschlüssel: 20 – 29 (Verwendungszweck), 35 - 36 (Empfängername), 60 - 63 (Verwendungszweck)
    /// </summary>
    [StringLength(10*27 + 2*27 + 4*27)]
    public string Description { get; set; }

    /// <summary>
    /// BIC Bankkennung Auftraggeber/ BIC
    ///
    /// Bankkennung Zahlungsempfänger
    ///
    /// Feldschlüssel: 30
    /// </summary>
    [StringLength(12)]
    public string BankCode { get; set; }

    /// <summary>
    /// IBAN Auftraggeber/ [AT01]
    /// IBAN Zahlungsempfänger [AT01]
    ///
    /// Feldschlüssel: 31
    /// </summary>
    [StringLength(34)]
    public string AccountCode { get; set; }

    /// <summary>
    /// Name Auftraggeber/ Name
    /// Zahlungsempfänger
    ///
    /// Feldschlüssel: 32 – 33
    ///
    /// [gekürzt: SEPA Länge = 70 Stellen]
    /// </summary>
    [StringLength(54)]
    public string PartnerName { get; set; }

    /// <summary>
    /// Textschlüsselergänzung
    ///
    /// Feldschlüssel: 34
    /// </summary>
    [StringLength(3)]
    public string TextKeyAddition { get; set; }

    #endregion

    #region SEPA Attributes (transmitted in purpose/description)

    // Die SEPA Attribute werden im Verwendungszweck mitgegeben.

    /// <summary>
    /// A list of all SEPA purposes passed over in the purpose/description field of the transaction.
    /// </summary>
    public Dictionary<SepaPurpose, string> SepaPurposes { get; set; } = new();

    /// <summary>
    /// SEPA attribute Ende-zu-Ende Referenz
    /// </summary>
    public string EREF { get; set; }

    /// <summary>
    /// SEPA attribute Kundenreferenz
    /// </summary>
    public string KREF { get; set; }

    /// <summary>
    /// SEPA attribute Mandatsreferenz
    /// </summary>
    public string MREF { get; set; }

    /// <summary>
    /// SEPA attribute Bankreferenz
    /// </summary>
    public string BREF { get; set; }

    /// <summary>
    /// SEPA attribute Retourenreferenz
    /// </summary>
    public string RREF { get; set; }

    /// <summary>
    /// SEPA attribute Creditor-ID
    /// </summary>
    public string CRED { get; set; }

    /// <summary>
    /// SEPA attribute Debitor-ID
    /// </summary>
    public string DEBT { get; set; }

    /// <summary>
    /// SEPA attribute Zinskompensationsbetrag
    /// </summary>
    public string COAM { get; set; }

    /// <summary>
    /// SEPA attribute Ursprünglicher Umsatzbetrag
    /// </summary>
    public string OAMT { get; set; }

    /// <summary>
    /// SEPA attribute Verwendungszweck
    /// </summary>
    public string SVWZ { get; set; }

    /// <summary>
    /// SEPA attribute Abweichender Auftraggeber
    /// </summary>
    public string ABWA { get; set; }

    /// <summary>
    /// SEPA attribute Abweichender Empfänger
    /// </summary>
    public string ABWE { get; set; }

    /// <summary>
    /// SEPA attribute IBAN des Auftraggebers
    /// </summary>
    public string IBAN { get; set; }

    /// <summary>
    /// SEPA attribute BIC des Auftraggebers
    /// </summary>
    public string BIC { get; set; }

    #endregion
}
