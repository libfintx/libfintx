using System;
using Xunit;
using System.Collections.Generic;
using libfintx.Sepa;
using libfintx.Sepa.Helper;


namespace libfintx.Tests.Pain;

public class pain00100103Tests
{
    [Fact]
    public void Test_Escape()
    {
        string str = SepaHelper.Escape(@"Hübner;;;\\\");
        Assert.Equal("Huebner", str);

        str = SepaHelper.Escape(@"Der Verwendungszweck der Überweisung ####ist die Mietzahlung.");
        Assert.Equal("Der Verwendungszweck der Ueberweisung ist die Mietzahlung.", str);
    }

    [Fact]
    public void Test_Escape_Removes_Invalid_Characters_Before_Xml_Escaping()
    {
        // '&' is not in the SEPA character set and is dropped, no "amp;" remains
        Assert.Equal("A  B", SepaHelper.Escape("A & B"));
        // the apostrophe is valid SEPA and must survive as XML entity
        Assert.Equal("O&apos;Neil", SepaHelper.Escape("O'Neil"));
        Assert.Null(SepaHelper.Escape(null));
    }

    [Fact]
    public void Create_Converts_Umlauts_Like_Other_Pain_Formats()
    {
        var result = pain00100103.Create("Müller & Söhne", "DE02120300000000202051", "BYLADEM1001",
            "Jürgen", "DE02120300000000202051", "BYLADEM1001", 1.5m, "Miete März", DateTime.Today);

        Assert.Contains("<Nm>Mueller  Soehne</Nm>", result);
        Assert.Contains("<Nm>Juergen</Nm>", result);
        Assert.Contains("<Ustrd>Miete Maerz</Ustrd>", result);
        Assert.DoesNotContain("amp", result);
    }

    [Fact]
    public void Create_Collective_Escapes_Names_And_Usage()
    {
        var data = new List<Pain00100203CtData>
        {
            new Pain00100203CtData
            {
                Receiver = "Jürgen <GmbH>", ReceiverIban = "DE02120300000000202051", ReceiverBic = "BYLADEM1001",
                Amount = 1m, Usage = "Rechnung ä"
            }
        };

        var result = pain00100103.Create("Müller", "DE02120300000000202051", "BYLADEM1001", data, "1", 1m, DateTime.Today);

        Assert.Contains("<Nm>Mueller</Nm>", result);
        Assert.Contains("<Nm>Juergen GmbH</Nm>", result);
        Assert.Contains("<Ustrd>Rechnung ae</Ustrd>", result);
    }

    [Fact(Skip = "You have to set the Arrange variables for this test")]
    public void Create_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        string Accountholder = null;
        string AccountholderIBAN = null;
        string AccountholderBIC = null;
        string Receiver = null;
        string ReceiverIBAN = null;
        string ReceiverBIC = null;
        decimal Amount = 0;
        string Usage = null;
        DateTime ExecutionDay = default(DateTime);

        // Act
        var result = pain00100103.Create(
            Accountholder,
            AccountholderIBAN,
            AccountholderBIC,
            Receiver,
            ReceiverIBAN,
            ReceiverBIC,
            Amount,
            Usage,
            ExecutionDay);

        // Assert
        Assert.True(false);
    }

    [Fact(Skip = "You have to set the Arrange variables for this test")]
    public void Create_StateUnderTest_ExpectedBehavior1()
    {
        // Arrange
        string Accountholder = null;
        string AccountholderIBAN = null;
        string AccountholderBIC = null;
        List<Pain00100203CtData> PainData = null;
        string NumberofTransactions = null;
        decimal TotalAmount = 0;
        DateTime ExecutionDay = default(DateTime);

        // Act
        var result = pain00100103.Create(
            Accountholder,
            AccountholderIBAN,
            AccountholderBIC,
            PainData,
            NumberofTransactions,
            TotalAmount,
            ExecutionDay);

        // Assert
        Assert.True(false);
    }
}
