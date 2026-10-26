using System.Linq;
using libfintx.FinTS;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// Segment related return values (HIRMS) and the outcome of a payment order. Return values as answered by banks,
/// values made up.
/// </summary>
public class Test_OrderStatus
{
    private static HBCIDialogResult Result(string inner)
    {
        var raw = $"HNHBK:1:3+000000000999+300+DIALOG1+2'HNVSK:998:3+PIN:2'HNVSD:999:1+@{inner.Length}@{inner}'HNHBS:5:1+2'";
        return new HBCIDialogResult(TestHelper.CreateTestClient().Parse_BankCode(raw), raw);
    }

    [Theory]
    // 0010 in HIRMG only means "message received"; the order is answered in HIRMS
    [InlineData("HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:4+0020::Auftrag ausgeführt.'", false, OrderStatus.Accepted)]
    [InlineData("HIRMG:2:2+0010::Nachricht entgegengenommen.'", false, OrderStatus.Unclear)]
    [InlineData("HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:5+3945::Freigabe kann nicht erteilt werden'", false, OrderStatus.NotExecuted)]
    [InlineData("HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:4+0010::Auftrag entgegengenommen.+0030::Sicherheitsfreigabe erforderlich'", false, OrderStatus.NotExecuted)]
    // a decoupled approval stays valid after the client stops asking (PIN/TAN 2020-07-10, p. 17 f.)
    [InlineData("HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:3+3956::Freigabe noch nicht erteilt'", false, OrderStatus.Unclear)]
    [InlineData("HIRMG:2:2+0010::Nachricht entgegengenommen.'HIRMS:3:2:4+3955::Sicherheitsfreigabe erfolgt über anderen Kanal'", false, OrderStatus.Unclear)]
    [InlineData("HIRMG:2:2+3955::Sicherheitsfreigabe erfolgt über anderen Kanal'HIRMS:3:2:4+0010::Auftrag entgegengenommen.'", false, OrderStatus.Unclear)]
    // collective orders: partly executed is neither success nor failure
    [InlineData("HIRMG:2:2+3060::Teilweise Warnungen.'HIRMS:3:2:4+3220::Auftrag ausgeführt, fehlerhafte Einzelpositionen'", false, OrderStatus.PartiallyAccepted)]
    [InlineData("HIRMG:2:2+9050::Teilweise fehlerhaft.'HIRMS:3:2:4+0020::Auftrag ausgeführt+9220::Einzelauftrag 2 nicht ausgeführt:2'", false, OrderStatus.PartiallyAccepted)]
    [InlineData("HIRMG:2:2+9050::Teilweise fehlerhaft.'HIRMS:3:2:4+9220::Einzelauftrag 2 nicht ausgeführt:2'", false, OrderStatus.Unclear)]
    [InlineData("HIRMG:2:2+9050::Teilweise fehlerhaft.'HIRMS:3:2:4+0020::Auftrag ausgeführt.'", false, OrderStatus.Unclear)]
    [InlineData("HIRMG:2:2+9050::Teilweise fehlerhaft.'HIRMS:3:2:3+9010::Fehler'HIRMS:4:2:4+0020::Auftrag ausgeführt.'", false, OrderStatus.Unclear)]
    [InlineData("HIRMG:2:2+9050::Teilweise fehlerhaft.'HIRMS:3:2:4+9210::Keine gültige Kontoverbindung.'", false, OrderStatus.NotExecuted)]
    [InlineData("HIRMG:2:2+9800::Dialog abgebrochen'", false, OrderStatus.NotExecuted)]
    // instant payments are accepted with 3045 (Geschäftsvorfälle 2022-04-15, C.10.2.9)
    [InlineData("HIRMG:2:2+3060::Hinweise.'HIRMS:3:2:3+3045::SEPA-Instant Payment Statusabfrage HKIPS veranlassen.'", true, OrderStatus.Accepted)]
    [InlineData("HIRMG:2:2+3060::Hinweise.'HIRMS:3:2:3+3045::SEPA-Instant Payment Statusabfrage HKIPS veranlassen.'", false, OrderStatus.Unclear)]
    public void Test_OrderStatusFromReturnValues(string inner, bool instantPayment, OrderStatus expected)
    {
        Assert.Equal(expected, Result(inner).GetOrderStatus(instantPayment));
    }

    [Fact]
    public void Test_ReturnValuesOfIgnoredSegmentsDoNotCount()
    {
        // HKVPP answers "0020 Auftrag ausgeführt" for the check (VOP 1.02, p. 13 f.), not for the payment
        var result = Result("HIRMS:3:2:3+0020::Namensabgleich ausgeführt.'HIRMS:4:2:5+0030::Sicherheitsfreigabe erforderlich'");
        Assert.Equal(OrderStatus.NotExecuted, result.GetOrderStatus());

        var onlyCheck = Result("HIRMS:3:2:3+0020::Namensabgleich ausgeführt.'");
        Assert.Equal(OrderStatus.Accepted, onlyCheck.GetOrderStatus());
        onlyCheck.IgnoredSegmentReferences.Add(3);
        Assert.Equal(OrderStatus.Unclear, onlyCheck.GetOrderStatus());

        // HKVPA 9210 "Auftrag existiert nicht bzw. wurde bereits ausgeführt" (VOP 1.02, p. 15): not "not executed"
        var executionOrderFailed = Result("HIRMG:2:2+9050::Teilweise fehlerhaft.'HIRMS:3:2:3+9210::Auftrag existiert nicht bzw. wurde bereits ausgeführt'");
        executionOrderFailed.IgnoredSegmentReferences.Add(3);
        Assert.Equal(OrderStatus.Unclear, executionOrderFailed.GetOrderStatus());
        var withNewChallenge = Result("HIRMS:3:2:3+9210::Auftrag existiert nicht bzw. wurde bereits ausgeführt'HIRMS:4:2:4+0030::Sicherheitsfreigabe erforderlich'");
        withNewChallenge.IgnoredSegmentReferences.Add(3);
        Assert.Equal(OrderStatus.Unclear, withNewChallenge.GetOrderStatus());

        var orderFailed = Result("HIRMS:3:2:3+9210::Fehler im Prüfauftrag'HIRMS:4:2:4+9210::Keine gültige Kontoverbindung'");
        orderFailed.IgnoredSegmentReferences.Add(3);
        Assert.Equal(OrderStatus.NotExecuted, orderFailed.GetOrderStatus());
    }

    [Fact]
    public void Test_SegmentReferenceAndParameters()
    {
        var result = Result("HIRMG:2:2+3060::Hinweise.'HIRMS:3:2:4+3040::Es liegen weitere Informationen vor.:A?:B+3220::Einzelposten fehlerhaft:2'");

        var dialog = result.Messages.Single(m => m.Code == "3060");
        Assert.Null(dialog.SegmentReference);
        var restart = result.Messages.Single(m => m.Code == "3040");
        Assert.Equal(4, restart.SegmentReference);
        Assert.Equal(new[] { "A:B" }, restart.Parameters);
        Assert.Equal(new[] { "3220" }, result.FailedItems.Select(m => m.Code));
    }
}
