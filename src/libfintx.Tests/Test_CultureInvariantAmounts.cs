using System;
using System.Globalization;
using System.Linq;
using libfintx.FinTS.Statement;
using libfintx.Sepa;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// FinTS and SWIFT amounts always use a decimal comma, whatever the current culture is.
/// </summary>
public class Test_CultureInvariantAmounts
{
    private const string HisalMessage = "HNHBK:1:3+000000000468+300+n4kfRJCpJK4D20210227110433521%+2+n4kfRJCpJK4D20210227110433521%:2'HNVSK:998:3+PIN:1+998+1+2::3791439560898000HYC8K5V5P7UPX9+1:20210227:110435+2:2:13:@8@        :5:1+280:70070010:XXXXXX:V:0:0+0'HNVSD:999:1+@212@HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:3+0020::Auftrag ausgeführt.'HISAL:4:7:3+DE02100701240123456789:DEUTDEDB101:123456789:EUR:280:10070124+SparCard+EUR+{0}:EUR:20260929:110435+D:12,34:EUR:20260929+5000,:EUR+12632,52:EUR''HNHBS:5:1+2'";

    private static void WithCulture(string culture, Action action)
    {
        WithCulture(culture == "" ? CultureInfo.InvariantCulture : new CultureInfo(culture), action);
    }

    private static void WithCulture(CultureInfo culture, Action action)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = culture;
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void Parse_Balance_Uses_Decimal_Comma(string culture)
    {
        WithCulture(culture, () =>
        {
            var client = TestHelper.CreateTestClient();

            var credit = client.Parse_Balance(string.Format(HisalMessage, "C:7632,52"));
            Assert.Equal(7632.52m, credit.Balance);
            Assert.Equal(-12.34m, credit.MarkedTransactions);
            Assert.Equal(5000m, credit.CreditLine);
            Assert.Equal(12632.52m, credit.AvailableBalance);

            var debit = client.Parse_Balance(string.Format(HisalMessage, "D:7632,52"));
            Assert.Equal(-7632.52m, debit.Balance);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void MT942_Amounts_Use_Decimal_Comma(string culture)
    {
        const string mt942 = @"
:20:STARTDISPE
:25:10050000/0123456789
:28C:00000/001
:34F:EURD1826,90
:13:1911071300
:61:1911081105DR1826,90NDDTNONREF
:86:105?00FOLGELASTSCHRIFT?109248?20EREF+Zahlbeleg
:90D:1EUR1826,90
:90C:0EUR0,00
-
";
        WithCulture(culture, () =>
        {
            var stmt = MT940.Deserialize(mt942, "0123456789", true).Single();

            Assert.Equal(-1826.90m, stmt.SmallestAmount);
            Assert.Equal(-1826.90m, stmt.SwiftTransactions.Single().Amount);
            Assert.Equal(-1826.90m, stmt.AmountDebit);
        });
    }

    [Fact]
    public void MT940_Balances_And_Amounts_Use_Decimal_Comma()
    {
        const string mt940 = @"
:20:STARTUMSE
:25:10070124/123456789
:28C:00000/001
:60F:C200101EUR1234,56
:61:200102D100,00NMSCNONREF
:86:116?20SVWZ+Debit
:61:200102RC12,50NMSCNONREF
:86:116?20SVWZ+Reversed credit
:62F:C200102EUR1122,06
";
        void AssertStatement()
        {
            var stmt = MT940.Deserialize(mt940, "123456789").Single();

            Assert.Equal(1234.56m, stmt.StartBalance);
            Assert.Equal(new[] { -100.00m, -12.50m }, stmt.SwiftTransactions.Select(t => t.Amount));
            Assert.Equal(1122.06m, stmt.EndBalance);
        }

        // Regression guard only: the old code swapped ',' for the culture's currency decimal separator,
        // which already worked under the invariant culture.
        WithCulture("", AssertStatement);

        // The old code broke here: it swapped in the currency decimal separator but Convert.ToDecimal
        // parses with the number decimal separator, so 1234,56 became 123456.
        var mixed = (CultureInfo) CultureInfo.GetCultureInfo("en-US").Clone();
        mixed.NumberFormat.CurrencyDecimalSeparator = ",";
        mixed.NumberFormat.CurrencyGroupSeparator = ".";
        WithCulture(mixed, AssertStatement);
    }

    [Fact]
    public void MT940_Malformed_Closing_Balance_Throws()
    {
        // The trailing '-' is on the :62F: line itself (no NUL after it), so the amount is "9999,99-",
        // which the decimal-comma parser rejects with a FormatException.
        const string mt940 = @"
:20:STARTUMSE
:25:10070124/123456789
:28C:00000/001
:60F:C200101EUR1234,56
:61:200102D100,00NMSCNONREF
:86:116?20SVWZ+Debit
:61:200102RC12,50NMSCNONREF
:86:116?20SVWZ+Reversed credit
:62F:C200102EUR9999,99-
";
        Assert.Throws<FormatException>(() => libfintx.Swift.MT940.Deserialize(mt940).ToList());
    }

    [Fact]
    public void Sepa_Amount_Uses_Decimal_Point()
    {
        // A culture whose decimal separator is neither ',' nor '.'
        var culture = (CultureInfo) CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = "/";

        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            var result = pain00100103.Create("Sender", "DE02120300000000202051", "BYLADEM1001",
                "Receiver", "DE02120300000000202051", "BYLADEM1001", 7632.52m, "Usage", DateTime.Today);

            Assert.Contains(">7632.52</InstdAmt>", result);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
