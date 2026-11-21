using System;
using libfintx.FinTS;
using libfintx.FinTS.Data;
using libfintx.Swift;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// The securities statement: the HKWPD request and the MT535 answer. The message is made up in
/// the MT535 layout German banks send in HIWPD: a general block, one block per instrument.
/// </summary>
[Collection("UPD")]
public class Test_SecuritiesStatement
{
    private const string Statement =
        ":16R:GENL\r\n" +
        ":28E:1/ONLY\r\n" +
        ":20C::SEME//NONREF\r\n" +
        ":23G:NEWM\r\n" +
        ":98C::STAT//20261112180000\r\n" +
        ":22F::STTY//CUST\r\n" +
        ":97A::SAFE//10050000/1234567890\r\n" +
        ":17B::ACTI//Y\r\n" +
        ":16S:GENL\r\n" +
        ":16R:FIN\r\n" +
        ":35B:ISIN IE00B4L5Y983\r\n" +
        "/DE/A0RPWH\r\n" +
        "ISHSIII-CORE MSCI WORLD U.ETF\r\n" +
        "REGISTERED SHS USD (ACC)\r\n" +
        ":90B::MRKT//ACTU/EUR104,12\r\n" +
        ":94B::PRIC//LMAR/XETR\r\n" +
        ":98A::PRIC//20261111\r\n" +
        ":93B::AGGR//UNIT/142,318\r\n" +
        ":16R:SUBBAL\r\n" +
        ":93C::TAVI//UNIT/AVAI/142,318\r\n" +
        ":16S:SUBBAL\r\n" +
        ":19A::HOLD//EUR14818,15\r\n" +
        ":70E::HOLD//1STK++++20261111+\r\n" +
        "284,6700000+EUR\r\n" +
        ":16S:FIN\r\n" +
        ":16R:FIN\r\n" +
        ":35B:ISIN DE0001102580\r\n" +
        "BUNDESREP.DEUTSCHLAND ANL.V.2022(2032)\r\n" +
        ":90A::MRKT//PRCT/97,85\r\n" +
        ":98A::PRIC//20261111\r\n" +
        ":93B::AGGR//FAMT/5000,\r\n" +
        ":19A::HOLD//EUR4892,50\r\n" +
        ":16S:FIN\r\n" +
        ":16R:ADDINFO\r\n" +
        ":19A::HOLD//EUR19710,65\r\n" +
        ":16S:ADDINFO\r\n" +
        "-";

    [Fact]
    public void TheStatement_GivesSharesPricesAndValues()
    {
        MT535 statement = new MT535Parser().Parse(Statement);

        Assert.Equal(new DateTime(2026, 11, 12), statement.StatementDate);
        Assert.Equal(2, statement.Holdings.Count);

        MT535Holding fund = statement.Holdings[0];
        Assert.Equal("IE00B4L5Y983", fund.Isin);
        Assert.Equal("ISHSIII-CORE MSCI WORLD U.ETF REGISTERED SHS USD (ACC)", fund.Name);
        Assert.Equal(142.318m, fund.Quantity);
        Assert.False(fund.IsNominal);
        Assert.Equal(104.12m, fund.Price);
        Assert.Equal("EUR", fund.PriceCurrency);
        Assert.Equal(new DateTime(2026, 11, 11), fund.PriceDate);
        Assert.Equal(14818.15m, fund.Value);
        Assert.Equal("EUR", fund.ValueCurrency);
    }

    [Fact]
    public void ABond_HasItsNominalAndAPriceInPercent()
    {
        MT535Holding bond = new MT535Parser().Parse(Statement).Holdings[1];

        Assert.Equal("DE0001102580", bond.Isin);
        Assert.True(bond.IsNominal);
        Assert.Equal(5000m, bond.Quantity);
        Assert.True(bond.PriceIsPercent);
        Assert.Equal(97.85m, bond.Price);
        Assert.Equal(4892.50m, bond.Value);
    }

    [Fact]
    public void TheGeneralBlock_AndFurtherFields_AreRead()
    {
        MT535 statement = new MT535Parser().Parse(Statement);

        Assert.Equal("NONREF", statement.Reference);
        Assert.Equal(1, statement.PageNumber);
        Assert.True(statement.IsLastPage);
        Assert.Equal("CUST", statement.StatementType);
        Assert.Equal("10050000/1234567890", statement.SafekeepingAccount);
        Assert.True(statement.HasHoldings);
        Assert.Equal(19710.65m, statement.TotalValue);
        Assert.Equal("EUR", statement.TotalValueCurrency);

        MT535Holding fund = statement.Holdings[0];
        Assert.Equal("A0RPWH", fund.Wkn);
        Assert.Equal(142.318m, fund.AvailableQuantity);
        Assert.Equal("ACTU", fund.PriceType);
        Assert.Equal("XETR", fund.PriceSource);
    }

    [Fact]
    public void TheAttributesOfABond_AreRead()
    {
        var statement = new MT535Parser().Parse(
            ":16R:FIN\n:35B:ISIN DE0001102580\nBUND\n:16R:FIA\n:11A::DENO//EUR\n:98A::MATU//20320215\n:92A::INTR//2,5\n:16S:FIA\n" +
            ":90A::MRKT//PRCT/97,85\n:93B::AGGR//FAMT/5000,\n:19A::ACRU//EUR31,25\n:92B::EXCH//EUR/USD/1,0812\n:16S:FIN\n-");

        MT535Holding bond = Assert.Single(statement.Holdings);
        Assert.Equal("EUR", bond.Denomination);
        Assert.Equal(new DateTime(2032, 2, 15), bond.MaturityDate);
        Assert.Equal(2.5m, bond.InterestRate);
        Assert.Equal(31.25m, bond.AccruedInterest);
        Assert.Equal("EUR", bond.AccruedInterestCurrency);
        Assert.Equal(1.0812m, bond.ExchangeRate);
        Assert.Equal("EUR/USD", bond.ExchangeRateCurrencies);
    }

    [Fact]
    public void AnAmountThatCannotBeRead_IsSkipped_NotTheStatement()
    {
        var statement = new MT535Parser().Parse(
            ":16R:FIN\n:35B:ISIN DE0008404005\nALLIANZ SE\n:93B::AGGR//UNIT/10,\n:90B::MRKT//ACTU/EUR1,2,3\n:16S:FIN\n-");

        MT535Holding holding = Assert.Single(statement.Holdings);
        Assert.Equal(10m, holding.Quantity);
        Assert.Null(holding.Price);
    }

    [Fact]
    public void APositionWithoutPrice_IsKeptWithoutOne()
    {
        var statement = new MT535Parser().Parse(":16R:FIN\n:35B:ISIN DE0008404005\nALLIANZ SE\n:93B::AGGR//UNIT/10,\n:16S:FIN\n-");

        MT535Holding holding = Assert.Single(statement.Holdings);
        Assert.Equal(10m, holding.Quantity);
        Assert.Null(holding.Price);
        Assert.Null(holding.Value);
    }

    [Fact]
    public void TheMt535_IsTakenFromTheBinaryOfHiwpd()
    {
        string mt535 = ":16R:FIN\r\n:35B:ISIN DE0008404005\r\nALLIANZ\r\n:16S:FIN\r\n-";
        string response = "HIRMG:2:2+0010::Nachricht entgegengenommen.'HIWPD:4:6:3+@" + mt535.Length + "@" + mt535 + "'HNHBS:5:1+1'";

        Assert.Equal(mt535 + "\n", FinTsClient.Mt535Of(response));
    }

    [Fact]
    public void TheRequest_NamesTheSecuritiesAccount()
    {
        var client = TestHelper.CreateTestClient(new ConnectionDetails { Account = "1234567890", Blz = 10050000 });
        client.HIWPDS = 6;

        Assert.Equal("HKWPD:3:6+1234567890::280:10050000'", HKWPD.Segment(client, null));
        Assert.Equal("HKWPD:3:6+1234567890::280:10050000++++ABC123'", HKWPD.Segment(client, "ABC123"));
    }
}
