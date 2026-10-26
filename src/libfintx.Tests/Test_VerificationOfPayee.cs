using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using libfintx.FinTS;
using libfintx.FinTS.BankParameterData;
using libfintx.FinTS.Data;
using libfintx.FinTS.Data.Segment;
using libfintx.Sepa;
using Xunit;

namespace libfintx.Tests;

/// <summary>
/// Verification of payee after "FinTS 3.0 Messages Geschäftsvorfälle VOP" 1.02 and the payment status report
/// pain.002.001.10 (DFÜ-Abkommen Anlage 3 V3.9, chapter 2.2.6). Bank answers rebuilt, values made up.
/// </summary>
public class Test_VerificationOfPayee
{
    private const string OwnIban = "DE89100700000099164406";
    private const string Payee = "DE02100700000123456789";
    private const string Payee2 = "DE02500105170137075030";
    private const string Psr = "urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.002.001.10";

    private const string Bpd = "HIBPA:5:3:3+12+280:10070000+Testbank+0+1+300+500'"
        + "HIPINS:78:1:4+1+1+0+5:20:6:Benutzerkennung:Kunden-ID:HKCCS:J:HKCCM:J:HKVPP:N:HKVPA:N'"
        + "HITANS:6:6:4+1+1+0+N:N:0:902:2:TAN1:::TAN-Verfahren:7:1:Freigabe:999:N:1:N:0:2:N:J:00:0:N:8'"
        + "HIVPPS:40:1:4+1+1+0+0:J:V:N:N:" + Psr + ":HKCCS:HKCCM'";

    private static string Message(string inner) => "HNHBK:1:3+000000000100+300+DIALOG1+2'" + inner + "HNHBS:9:1+2'";

    private static string Wrapped(string inner) =>
        $"HNHBK:1:3+000000000999+300+DIALOG1+2'HNVSK:998:3+PIN:2'HNVSD:999:1+@{inner.Length}@{inner}'HNHBS:5:1+2'";

    [Fact]
    public void Test_Hivpps()
    {
        var bpd = BPD.Parse(Bpd);

        Assert.Equal(new[] { "HKCCS", "HKCCM" }, bpd.HIVPPS.PaymentOrders);
        Assert.Equal(new[] { "urn:iso:std:iso:20022:tech:xsd:pain.002.001.10" }, bpd.HIVPPS.PaymentStatusReportFormats);
        Assert.True(bpd.HIVPPS.ExplanationStructured);
        Assert.False(bpd.HIVPPS.CollectiveWithOneTransactionAllowed);
        Assert.Equal(0, bpd.HIVPPS.MaxTransactions);
        Assert.Null(BPD.Parse("HIBPA:5:3:3+12+280:10070000+Testbank+0+1+300+500'").HIVPPS);
    }

    [Fact]
    public void Test_Segments()
    {
        var p = BPD.Parse(Bpd).HIVPPS;
        Assert.Equal("HKVPP:3:1+" + Psr + "'", HKVPP.Create(3, p));
        Assert.Equal("HKVPP:3:1+" + Psr + "+@3@P'1++AUF?:1'", HKVPP.Create(3, p, new DataElementValue("P'1", true), "AUF:1"));
        // the VOP id goes back unchanged as binary data, also with separators in it (VOP 1.02, pp. 14, 27)
        Assert.Equal("HKVPA:3:1+@8@ID+:'@?ä'", HKVPA.Create(3, "ID+:'@?ä"));
    }

    [Fact]
    public void Test_SingleResultCloseMatch()
    {
        var answer = VerificationOfPayee.ParseAnswer(Wrapped(
            "HIRMS:3:2:3+3090::Ergebnis Namensabgleich prüfen+3945::Freigabe kann nicht erteilt werden'"
            + "HIVPP:4:1:3+@6@VOP-77+20261026:130000++++" + Payee + "::Erika Mustermann-Beispiel::RVMC"
            + "+Wenn Sie trotzdem freigeben, <b>haftet</b> die Bank nicht.<br>Bitte prüfen.'"));

        Assert.Equal("VOP-77", answer.VopId.Text);
        var result = VerificationOfPayee.Evaluate(answer, new[] { ("Erika Mustermann", Payee, "M-1") }, new List<byte[]>(), true, true, false);
        var item = Assert.Single(result.Items);
        Assert.Equal(VerificationOfPayeeMatch.CloseMatch, item.Result);
        Assert.Equal("Erika Mustermann-Beispiel", item.NameAtBank);
        Assert.False(result.AllMatch);
        Assert.Equal("Wenn Sie trotzdem freigeben, haftet die Bank nicht.\nBitte prüfen.", result.ExplanationPlainText);
    }

    [Fact]
    public void Test_CollectiveResultFromPaymentStatusReport()
    {
        // Only deviations on transaction level (item 1 missing = match); RVMC: name of the payee's bank in the first
        // AddtlInf, continued with a leading quotation mark; RVNA: reason AG03; file level information starts with the
        // status. The report comes in two parts.
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Document xmlns=""urn:iso:std:iso:20022:tech:xsd:pain.002.001.10""><CstmrPmtStsRpt>
<GrpHdr><MsgId>R1</MsgId><CreDtTm>2026-10-26T10:00:00</CreDtTm></GrpHdr>
<OrgnlGrpInfAndSts><OrgnlMsgId>M</OrgnlMsgId><OrgnlMsgNmId>pain.001.001.09</OrgnlMsgNmId><GrpSts>RVCM</GrpSts>
  <StsRsnInf><AddtlInf>RVMC Der Name weicht leicht ab.</AddtlInf><AddtlInf>RVNA Eine Prüfung war nicht möglich.</AddtlInf></StsRsnInf></OrgnlGrpInfAndSts>
<OrgnlPmtInfAndSts><OrgnlPmtInfId>M</OrgnlPmtInfId>
<TxInfAndSts><OrgnlEndToEndId>M-2</OrgnlEndToEndId><TxSts>RVMC</TxSts>
  <StsRsnInf><AddtlInf>Max Muster GmbH &amp; Co. KG Niederlassung</AddtlInf><AddtlInf>”Musterstadt</AddtlInf></StsRsnInf>
  <OrgnlTxRef><Cdtr><Pty><Nm>Max Muster GmbH</Nm></Pty></Cdtr><CdtrAcct><Id><IBAN>" + Payee2 + @"</IBAN></Id></CdtrAcct></OrgnlTxRef></TxInfAndSts>
<TxInfAndSts><OrgnlEndToEndId>M-3</OrgnlEndToEndId><TxSts>RVNA</TxSts>
  <StsRsnInf><Rsn><Cd>AG03</Cd></Rsn><AddtlInf>- Empfängerbank nimmt nicht teil</AddtlInf></StsRsnInf>
  <OrgnlTxRef><Cdtr><Pty><Nm>Firma Beispiel</Nm></Pty></Cdtr><CdtrAcct><Id><IBAN>DE75512108001245126199</IBAN></Id></CdtrAcct></OrgnlTxRef></TxInfAndSts>
</OrgnlPmtInfAndSts></CstmrPmtStsRpt></Document>";
        var bytes = Encoding.UTF8.GetBytes(xml);
        var payees = new[] { ("Erika Mustermann", Payee, "M-1"), ("Max Muster GmbH", Payee2, "M-2"), ("Firma Beispiel", "DE75512108001245126199", "M-3") };

        var result = VerificationOfPayee.Evaluate(new VerificationOfPayeeAnswer { Explanation = "Hinweis" }, payees,
            new[] { bytes.Take(200).ToArray(), bytes.Skip(200).ToArray() }, false, true, false);

        Assert.Equal(new[] { VerificationOfPayeeMatch.Match, VerificationOfPayeeMatch.CloseMatch, VerificationOfPayeeMatch.NotApplicable },
            result.Items.Select(i => i.Result));
        Assert.Equal("Max Muster GmbH & Co. KG NiederlassungMusterstadt", result.Items[1].NameAtBank);
        Assert.Equal("Der Name weicht leicht ab.", result.Items[1].Information);
        Assert.Equal("AG03", result.Items[2].ReasonCode);
        Assert.Equal("Empfängerbank nimmt nicht teil Eine Prüfung war nicht möglich.", result.Items[2].Information);
    }

    [Fact]
    public void Test_StepByStepDeliveryAndLongName()
    {
        // later reports only name transactions that are still not a match: a PDNG item not named any more is a match
        static byte[] Report(string tx) => Encoding.UTF8.GetBytes(
            @"<?xml version=""1.0"" encoding=""UTF-8""?><Document xmlns=""urn:iso:std:iso:20022:tech:xsd:pain.002.001.10""><CstmrPmtStsRpt><OrgnlPmtInfAndSts>"
            + tx + "</OrgnlPmtInfAndSts></CstmrPmtStsRpt></Document>");
        var first = Report("<TxInfAndSts><OrgnlEndToEndId>M-1</OrgnlEndToEndId><TxSts>PDNG</TxSts></TxInfAndSts>"
                           + "<TxInfAndSts><OrgnlEndToEndId>M-2</OrgnlEndToEndId><TxSts>PDNG</TxSts></TxInfAndSts>");
        var second = Report("<TxInfAndSts><OrgnlEndToEndId>M-2</OrgnlEndToEndId><TxSts>RVMC</TxSts><StsRsnInf>"
                            + "<AddtlInf>Max Muster GmbH Nieder</AddtlInf><AddtlInf>” lassung Musterstadt</AddtlInf></StsRsnInf></TxInfAndSts>");

        var result = VerificationOfPayee.Evaluate(new VerificationOfPayeeAnswer(), new[] { ("A", Payee, "M-1"), ("B", Payee2, "M-2") },
            new[] { first, second }, false, true, false);

        Assert.Equal(new[] { VerificationOfPayeeMatch.Match, VerificationOfPayeeMatch.CloseMatch }, result.Items.Select(i => i.Result));
        Assert.Equal("Max Muster GmbH Nieder lassung Musterstadt", result.Items[1].NameAtBank);
    }

    [Fact]
    public void Test_UnreadableReportIsNoMatch()
    {
        var payees = new[] { ("A", Payee, "M-1") };
        var broken = Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><Document><CstmrPmtStsRpt><TxInfAndSts>");

        var result = VerificationOfPayee.Evaluate(new VerificationOfPayeeAnswer(), payees, new[] { broken }, false, true, false);

        Assert.Equal(VerificationOfPayeeMatch.Unknown, Assert.Single(result.Items).Result);
    }

    [Fact]
    public void Test_UnassignedReportEntryIsNoMatch()
    {
        // the report names a transaction that matches no payee: the others are not known to match
        var report = Encoding.UTF8.GetBytes(@"<?xml version=""1.0"" encoding=""UTF-8""?><Document xmlns=""urn:iso:std:iso:20022:tech:xsd:pain.002.001.10""><CstmrPmtStsRpt><OrgnlPmtInfAndSts>"
            + "<TxInfAndSts><OrgnlEndToEndId>X-9</OrgnlEndToEndId><TxSts>RVNM</TxSts></TxInfAndSts></OrgnlPmtInfAndSts></CstmrPmtStsRpt></Document>");

        var result = VerificationOfPayee.Evaluate(new VerificationOfPayeeAnswer(), new[] { ("A", Payee, "M-1"), ("B", Payee2, "M-2") },
            new[] { report }, false, true, false);

        Assert.All(result.Items, i => Assert.Equal(VerificationOfPayeeMatch.Unknown, i.Result));
    }

    [Fact]
    public void Test_NotProvidedEndToEndIdsAreAssignedByIban()
    {
        // pain formats before .09 send NOTPROVIDED: the transactions must not overwrite each other
        var report = Encoding.UTF8.GetBytes(@"<?xml version=""1.0"" encoding=""UTF-8""?><Document xmlns=""urn:iso:std:iso:20022:tech:xsd:pain.002.001.10""><CstmrPmtStsRpt><OrgnlPmtInfAndSts>"
            + "<TxInfAndSts><OrgnlEndToEndId>NOTPROVIDED</OrgnlEndToEndId><TxSts>RVNM</TxSts><OrgnlTxRef><CdtrAcct><Id><IBAN>" + Payee + "</IBAN></Id></CdtrAcct></OrgnlTxRef></TxInfAndSts>"
            + "<TxInfAndSts><OrgnlEndToEndId>NOTPROVIDED</OrgnlEndToEndId><TxSts>RVNA</TxSts><OrgnlTxRef><CdtrAcct><Id><IBAN>" + Payee2 + "</IBAN></Id></CdtrAcct></OrgnlTxRef></TxInfAndSts>"
            + "</OrgnlPmtInfAndSts></CstmrPmtStsRpt></Document>");

        var result = VerificationOfPayee.Evaluate(new VerificationOfPayeeAnswer(), new[] { ("A", Payee, "NOTPROVIDED"), ("B", Payee2, "NOTPROVIDED") },
            new[] { report }, false, true, false);

        Assert.Equal(new[] { VerificationOfPayeeMatch.NoMatch, VerificationOfPayeeMatch.NotApplicable }, result.Items.Select(i => i.Result));
    }

    [Fact]
    public void Test_WithoutResultTheReturnValueCounts()
    {
        var payees = new[] { ("A", Payee, "M-1") };
        Assert.Equal(VerificationOfPayeeMatch.Match,
            VerificationOfPayee.Evaluate(new VerificationOfPayeeAnswer(), payees, new List<byte[]>(), false, false, true).Items[0].Result);
        Assert.Equal(VerificationOfPayeeMatch.Unknown,
            VerificationOfPayee.Evaluate(new VerificationOfPayeeAnswer(), payees, new List<byte[]>(), false, false, false).Items[0].Result);
    }

    // ---- complete flows against the fake bank (VOP 1.02, appendix E.8.1) ----

    private static FinTsClient CreateClient(FakeFinTsServer server)
    {
        var client = TestHelper.CreateTestClient(new ConnectionDetails
        {
            Url = server.Url,
            Blz = 10070000,
            UserId = "user",
            Pin = "pin",
            CustomerSystemId = "SYS",
            Iban = OwnIban,
            Bic = "DEUTDEFFXXX",
            AccountHolder = "Erika Mustermann",
        });
        client.BPD = BPD.Parse(Bpd);
        client.HISPAS_Pain = 4;
        client.HITANS = 6;
        client.HIRMS = "902";
        return client;
    }

    private const string TanRequired = "HIRMS:4:2:5+0030::Auftrag empfangen - Sicherheitsfreigabe erforderlich'HITAN:5:6:5+4++REF-1+Bitte TAN eingeben'";
    private const string Executed = "HIRMS:3:2:3+0020::Namensabgleich ausgeführt'HIRMS:4:2:4+0020::Auftrag ausgeführt.'";

    /// <summary>Fake bank: answers by the segments of the request.</summary>
    private static FakeFinTsServer Bank(string check, string confirm = null, string polling = null) => new FakeFinTsServer(request =>
        request.Contains("HKVPP:3:1") && request.Contains("HKCCS:4:1") ? Message(check)
        : request.Contains("HKVPA:3:1") && request.Contains("HKCCS:4:1") ? Message(confirm ?? TanRequired)
        : request.Contains("HKVPP:3:1") ? Message(polling)
        : request.Contains("HKTAN:") ? Message(Executed)
        : FakeFinTsServer.DefaultResponse);

    private static (TANDialog Dialog, List<VerificationOfPayeeResult> Shown) Dialog(bool consent)
    {
        var shown = new List<VerificationOfPayeeResult>();
        var dialog = new TANDialog(_ => Task.FromResult("123456"))
        {
            ConfirmVerificationOfPayeeAsync = r =>
            {
                shown.Add(r);
                return Task.FromResult(consent);
            },
        };
        return (dialog, shown);
    }

    private static Task<HBCIDialogResult> Transfer(FinTsClient client, TANDialog dialog) =>
        client.Transfer(dialog, "Max Muster", Payee, null, 1m, "Rechnung", null);

    private static string Order(string request) => Regex.Match(request, @"HKCCS:4:1\+.*?</Document>'", RegexOptions.Singleline).Value;

    [Fact]
    public async Task Test_MatchConfirmsWithTheTan()
    {
        // neither 3945 nor 3091: HKVPA goes together with the TAN (figure 3)
        using var server = Bank("HIRMS:3:2:3+0020::Namensabgleich ausgeführt'" + TanRequired + "HIVPP:6:1:3+@4@V-01+++++" + Payee + "::::RCVC'");
        var (dialog, shown) = Dialog(true);

        var result = await Transfer(CreateClient(server), dialog);

        Assert.Equal(VerificationOfPayeeMatch.Match, Assert.Single(Assert.Single(shown).Items).Result);
        Assert.False(shown[0].ConfirmationRequired);
        var tan = server.Requests.Last();
        Assert.Contains("HKVPA:3:1+@4@V-01'", tan);
        Assert.Contains("HKTAN:4:6+2++++REF-1+N'", tan);
        Assert.Equal(OrderStatus.Accepted, result.GetOrderStatus());
    }

    [Fact]
    public async Task Test_DeviationNeedsConsentAndSendsTheSameOrderAgain()
    {
        // 3945: the TAN is invalidated; after consent HKVPA + the same order + a new HKTAN
        using var server = Bank("HIRMS:3:2:3+3090::Ergebnis Namensabgleich prüfen'HIRMS:4:2:5+3945::Freigabe kann nicht erteilt werden'"
            + "HIVPP:6:1:3+@4@V-02+++++" + Payee + "::Max Mustermann::RVMC+Bitte prüfen'");
        var (dialog, shown) = Dialog(true);

        var result = await Transfer(CreateClient(server), dialog);

        var check = Assert.Single(shown);
        Assert.True(check.ConfirmationRequired);
        Assert.Equal("Max Mustermann", check.Items[0].NameAtBank);
        var first = server.Requests.First(r => r.Contains("HKVPP:3:1"));
        var again = server.Requests.First(r => r.Contains("HKVPA:3:1+@4@V-02'"));
        Assert.Equal(Order(first), Order(again));
        Assert.Equal(OrderStatus.Accepted, result.GetOrderStatus());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Test_DeviationWithoutConsentIsNotExecuted(bool withCallback)
    {
        using var server = Bank("HIRMS:4:2:5+3945::Freigabe kann nicht erteilt werden'HIVPP:6:1:3+@4@V-03+++++" + Payee + "::::RVNM'");
        var dialog = withCallback ? Dialog(false).Dialog : new TANDialog(_ => Task.FromResult("123456"));

        var result = await Transfer(CreateClient(server), dialog);

        Assert.Equal(OrderStatus.NotExecuted, result.GetOrderStatus());
        Assert.Equal(VerificationOfPayeeMatch.NoMatch, dialog.VerificationOfPayee.Items[0].Result);
        Assert.Contains("HKCCS:4:1", server.Requests.Last());   // nothing sent after the order
        Assert.DoesNotContain(server.Requests, r => r.Contains("HKVPA"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Test_ExecutionOrderErrorIsUnclear(bool afterDeviation)
    {
        // HKVPA 9210 "Auftrag existiert nicht bzw. wurde bereits ausgeführt" (VOP 1.02, p. 15): resubmitting could pay twice
        const string failed = "HIRMG:2:2+9050::Teilweise fehlerhaft.'HIRMS:3:2:3+9210::Auftrag existiert nicht bzw. wurde bereits ausgeführt'";
        var check = afterDeviation
            ? "HIRMS:4:2:5+3945::Freigabe kann nicht erteilt werden'HIVPP:6:1:3+@4@V-06+++++" + Payee + "::Max Mustermann::RVMC+Bitte prüfen'"
            : "HIRMS:3:2:3+0020::Namensabgleich ausgeführt'" + TanRequired + "HIVPP:6:1:3+@4@V-06+++++" + Payee + "::::RCVC'";
        using var server = new FakeFinTsServer(request =>
            request.Contains("HKVPP:3:1") && request.Contains("HKCCS:4:1") ? Message(check)
            : request.Contains("HKVPA:3:1") ? Message(failed)
            : FakeFinTsServer.DefaultResponse);

        var result = await Transfer(CreateClient(server), Dialog(true).Dialog);

        Assert.Contains(server.Requests, r => r.Contains("HKVPA:3:1+@4@V-06'"));
        Assert.Equal(OrderStatus.Unclear, result.GetOrderStatus());
    }

    [Fact]
    public async Task Test_WithoutExecutionOrderOnlyTheTan()
    {
        // 3091: no confirmation needed, only answer the challenge
        using var server = Bank("HIRMS:4:2:5+3091::Ausführung ohne Bestätigung'" + TanRequired + "HIVPP:6:1:3+@4@V-04+++++" + Payee + "::::RCVC'");

        var result = await Transfer(CreateClient(server), Dialog(true).Dialog);

        Assert.DoesNotContain(server.Requests, r => r.Contains("HKVPA"));
        Assert.Equal(OrderStatus.Accepted, result.GetOrderStatus());
    }

    [Fact]
    public async Task Test_AlreadyExecutedWithoutTanStaysAccepted()
    {
        // 3076: executed without strong authentication; refusing afterwards cannot stop it any more
        using var server = Bank("HIRMS:3:2:3+0020::Namensabgleich ausgeführt'HIRMS:4:2:4+0020::Auftrag ausgeführt.'"
            + "HIRMS:5:2:5+3076::Keine starke Authentifizierung erforderlich+3091::Ausführung ohne Bestätigung'HIVPP:6:1:3+@4@V-05+++++" + Payee + "::::RCVC'");

        var result = await Transfer(CreateClient(server), Dialog(false).Dialog);

        Assert.Equal(OrderStatus.Accepted, result.GetOrderStatus());
        Assert.Contains("HKCCS:4:1", server.Requests.Last());   // nothing sent after the order
        Assert.DoesNotContain(server.Requests, r => r.Contains("HKVPA"));
    }

    [Fact]
    public async Task Test_FurtherResultsWithPollingId()
    {
        // 3040: HKVPP alone with polling id and restart point until the VOP id comes
        using var server = Bank("HIRMS:3:2:3+3040::Es liegen weitere Informationen vor.:AUF1'" + TanRequired + "HIVPP:6:1:3+++@2@P1+++++1'",
            polling: "HIVPP:3:1:3+@4@V-06+++++" + Payee + "::::RCVC'");

        var result = await Transfer(CreateClient(server), Dialog(true).Dialog);

        Assert.Contains(server.Requests, r => r.Contains("HKVPP:3:1+" + Psr + "+@2@P1++AUF1'") && !r.Contains("HKCCS"));
        Assert.Contains("HKVPA:3:1+@4@V-06'", server.Requests.Last());
        Assert.Equal(OrderStatus.Accepted, result.GetOrderStatus());
    }

    [Fact]
    public async Task Test_MissingAnswerAfterTheTanIsUncertain()
    {
        // the TAN went out, the bank does not answer: the payment may be executed, so never "not executed"
        using var server = new FakeFinTsServer(request =>
            request.Contains("HKVPP:3:1") && request.Contains("HKCCS:4:1")
                ? Message("HIRMS:3:2:3+0020::Namensabgleich ausgeführt'" + TanRequired + "HIVPP:6:1:3+@4@V-08+++++" + Payee + "::::RCVC'")
            : request.Contains("HKVPA:3:1") ? null
            : FakeFinTsServer.DefaultResponse);

        await Assert.ThrowsAsync<PaymentOrderUncertainException>(() => Transfer(CreateClient(server), Dialog(true).Dialog));
    }

    [Fact]
    public async Task Test_ErrorWhileFetchingResultsKeepsStartedApprovalUnclear()
    {
        // a decoupled approval was already started (3955); an error while fetching further results must not say "not executed"
        using var server = Bank("HIRMS:3:2:3+3040::Es liegen weitere Informationen vor.:AUF1'HIRMS:4:2:5+3955::Sicherheitsfreigabe erfolgt über anderen Kanal'"
            + "HITAN:5:7:5+4++REF-1+Bitte in der App freigeben'HIVPP:6:1:3+++@2@P1+++++1'",
            polling: "HIRMG:2:2+9050::Teilweise fehlerhaft.'HIRMS:3:2:3+9010::Verarbeitung nicht möglich'");

        var result = await Transfer(CreateClient(server), Dialog(true).Dialog);

        Assert.Equal(OrderStatus.Unclear, result.GetOrderStatus());
    }

    [Fact]
    public async Task Test_CollectiveWithOneTransactionGoesAsSingleTransfer()
    {
        using var server = Bank("HIRMS:3:2:3+0020::Namensabgleich ausgeführt'" + TanRequired + "HIVPP:6:1:3+@4@V-07+++++" + Payee + "::::RCVC'");
        var items = new List<Pain00100203CtData> { new Pain00100203CtData { Receiver = "Max Muster", ReceiverIban = Payee, Amount = 1m, Usage = "x" } };

        await CreateClient(server).CollectiveTransfer(Dialog(true).Dialog, items, "1", 1m, null);

        Assert.Contains(server.Requests, r => r.Contains("HKCCS:4:1"));
        Assert.DoesNotContain(server.Requests, r => r.Contains("HKCCM"));
    }

    [Fact]
    public async Task Test_MaximumNumberOfPayees()
    {
        using var server = Bank("");
        var client = CreateClient(server);
        client.BPD = BPD.Parse(Bpd.Replace("HIVPPS:40:1:4+1+1+0+0:", "HIVPPS:40:1:4+1+1+0+1:"));
        var items = new List<Pain00100203CtData>
        {
            new Pain00100203CtData { Receiver = "A", ReceiverIban = Payee, Amount = 1m, Usage = "1" },
            new Pain00100203CtData { Receiver = "B", ReceiverIban = Payee2, Amount = 1m, Usage = "2" },
        };

        await Assert.ThrowsAsync<ArgumentException>(() => client.CollectiveTransfer(Dialog(true).Dialog, items, "2", 2m, null));
    }
}
