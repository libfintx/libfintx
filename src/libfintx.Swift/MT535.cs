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

#nullable enable

using System;
using System.Collections.Generic;

namespace libfintx.Swift;

/// <summary>
/// A securities statement in SWIFT MT535 format (Depotaufstellung): the general block with the
/// day and the account, the positions, and the total value the bank reports.
/// </summary>
public sealed class MT535
{
    /// <summary>The day the statement is for (:98A::STAT or :98C::STAT).</summary>
    public DateTime? StatementDate { get; set; }

    /// <summary>When the bank prepared the statement (:98A::PREP or :98C::PREP), if it says.</summary>
    public DateTime? PreparationDate { get; set; }

    /// <summary>The bank's reference of the statement (:20C::SEME), such as "NONREF".</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>The page of a statement sent in parts (:28E:), or null when the bank sends none.</summary>
    public int? PageNumber { get; set; }

    /// <summary>True when this is the last or only page (:28E: "LAST" or "ONLY").</summary>
    public bool IsLastPage { get; set; } = true;

    /// <summary>The kind of statement (:22F::STTY), such as "CUST" for a customer statement.</summary>
    public string StatementType { get; set; } = string.Empty;

    /// <summary>The safekeeping account the statement is for (:97A::SAFE), as the bank writes it, e.g. "10050000/1234567890".</summary>
    public string SafekeepingAccount { get; set; } = string.Empty;

    /// <summary>Whether the account had any holdings (:17B::ACTI), or null when the bank does not say.</summary>
    public bool? HasHoldings { get; set; }

    /// <summary>The value of all positions (:19A::HOLD in the general or additional block), or null.</summary>
    public decimal? TotalValue { get; set; }

    /// <summary>The currency of <see cref="TotalValue"/>.</summary>
    public string TotalValueCurrency { get; set; } = string.Empty;

    /// <summary>The positions, one per financial instrument.</summary>
    public List<MT535Holding> Holdings { get; } = new List<MT535Holding>();
}

/// <summary>One position of a securities statement, as far as the bank reports it.</summary>
public sealed class MT535Holding
{
    /// <summary>The ISIN (:35B:).</summary>
    public string Isin { get; set; } = string.Empty;

    /// <summary>The German securities code (WKN) from a "/DE/" line of :35B:, if the bank sends one.</summary>
    public string Wkn { get; set; } = string.Empty;

    /// <summary>The name of the security (:35B:, the lines after the codes).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Shares, or for bonds the nominal amount (see <see cref="IsNominal"/>) (:93B::AGGR).</summary>
    public decimal Quantity { get; set; }

    /// <summary>True when <see cref="Quantity"/> is a nominal amount (FAMT) rather than a number of shares (UNIT).</summary>
    public bool IsNominal { get; set; }

    /// <summary>The part of the quantity that is available, from a sub-balance (:93C::TAVI//…/AVAI/), or null.</summary>
    public decimal? AvailableQuantity { get; set; }

    /// <summary>The price, or null when the bank sends none (:90B::MRKT or :90A::MRKT).</summary>
    public decimal? Price { get; set; }

    /// <summary>The currency of <see cref="Price"/>; empty for a price in percent.</summary>
    public string PriceCurrency { get; set; } = string.Empty;

    /// <summary>True when the price is a percentage of the nominal amount, as for bonds.</summary>
    public bool PriceIsPercent { get; set; }

    /// <summary>The kind of price the bank gives, such as "ACTU" (actual) or "PRCT" (percent).</summary>
    public string PriceType { get; set; } = string.Empty;

    /// <summary>When the price was fixed (:98A::PRIC or :98C::PRIC), or null.</summary>
    public DateTime? PriceDate { get; set; }

    /// <summary>Where the price comes from (:94B::PRIC), such as the exchange code "XETR", or empty.</summary>
    public string PriceSource { get; set; } = string.Empty;

    /// <summary>The value of the position, or null when the bank sends none (:19A::HOLD).</summary>
    public decimal? Value { get; set; }

    /// <summary>The currency of <see cref="Value"/>.</summary>
    public string ValueCurrency { get; set; } = string.Empty;

    /// <summary>Interest accrued on a bond (:19A::ACRU), or null.</summary>
    public decimal? AccruedInterest { get; set; }

    /// <summary>The currency of <see cref="AccruedInterest"/>.</summary>
    public string AccruedInterestCurrency { get; set; } = string.Empty;

    /// <summary>The exchange rate the bank used to value the position (:92B::EXCH), or null.</summary>
    public decimal? ExchangeRate { get; set; }

    /// <summary>The currencies of <see cref="ExchangeRate"/>, as the bank names them, e.g. "EUR/USD".</summary>
    public string ExchangeRateCurrencies { get; set; } = string.Empty;

    /// <summary>The acquisition price per share the bank reports (:70E::HOLD), if any. Informational only.</summary>
    public decimal? AcquisitionPrice { get; set; }

    /// <summary>The currency of <see cref="AcquisitionPrice"/>.</summary>
    public string AcquisitionPriceCurrency { get; set; } = string.Empty;

    /// <summary>The currency the security is denominated in (:11A::DENO in the attributes), or empty.</summary>
    public string Denomination { get; set; } = string.Empty;

    /// <summary>The maturity of a bond (:98A::MATU in the attributes), or null.</summary>
    public DateTime? MaturityDate { get; set; }

    /// <summary>The interest rate of a bond in percent (:92A::INTR in the attributes), or null.</summary>
    public decimal? InterestRate { get; set; }
}
