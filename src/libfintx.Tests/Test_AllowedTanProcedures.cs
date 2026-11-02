using System;
using libfintx.FinTS.Exceptions;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// The TAN procedure set by the caller is checked against the procedures the bank allows for the user (3920).
/// </summary>
public class Test_AllowedTanProcedures
{
    private const string Answer =
        "HNHBK:1:3+000000000100+300+DIALOG+1+DIALOG:1'"
        + "HIRMG:2:2+3060::Bitte beachten Sie die enthaltenen Warnungen/Hinweise.'"
        + "HIRMS:3:2:4+3920::Zugelassene Zwei-Schritt-Verfahren für den Benutzer.:921:942'"
        + "HNHBS:4:1+1'";

    [Theory]
    [InlineData("921")]
    [InlineData("942")]
    public void Test_AllowedProcedureIsAccepted(string tanProcedure)
    {
        var client = TestHelper.CreateTestClient();
        client.HIRMS = tanProcedure;

        client.Parse_Segments(Answer);

        Assert.Equal(tanProcedure, client.HIRMS);
    }

    [Theory]
    [InlineData("92")] // part of 921
    [InlineData("42")] // part of 942
    [InlineData("944")]
    public void Test_OtherProcedureIsRefused(string tanProcedure)
    {
        var client = TestHelper.CreateTestClient();
        client.HIRMS = tanProcedure;

        var exception = Assert.ThrowsAny<Exception>(() => client.Parse_Segments(Answer));

        var refused = Assert.IsType<TanProcedureNotAllowedException>(exception.InnerException);
        Assert.Equal(tanProcedure, refused.TanProcedure);
        Assert.Equal(new[] { "921", "942" }, refused.AllowedTanProcedures);
        Assert.Contains("HIRMS/Tan-Mode", refused.Message);
    }

    [Fact]
    public void Test_WithoutProcedureTheFirstAllowedIsTaken()
    {
        var client = TestHelper.CreateTestClient();

        client.Parse_Segments(Answer);

        Assert.Equal("921", client.HIRMS);
    }
}
