using System.Linq;
using System.Text;
using libfintx.FinTS.Camt.Camt052;
using Xunit;

namespace libfintx.Tests.FinTS.Camt.Camt052;

/// <summary>
/// A camt.052.001.08 report as banks send it during the day: no currency on the account, an
/// interim booked balance in place of a closing one, a payment through a payment service and an
/// entry without transaction details.
/// </summary>
public class Camt052ReportTest
{
    private const string Report = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.052.001.08">
          <BkToCstmrAcctRpt>
            <GrpHdr><MsgId>M1</MsgId><CreDtTm>2026-11-16T10:00:00</CreDtTm></GrpHdr>
            <Rpt>
              <Id>R1</Id>
              <CreDtTm>2026-11-16T10:00:00</CreDtTm>
              <Acct><Id><IBAN>DE02430609670000000001</IBAN></Id><Svcr><FinInstnId><BICFI>GENODEM1GLS</BICFI></FinInstnId></Svcr></Acct>
              <Bal><Tp><CdOrPrtry><Cd>PRCD</Cd></CdOrPrtry></Tp><Amt Ccy="EUR">100.00</Amt><CdtDbtInd>CRDT</CdtDbtInd><Dt><Dt>2026-11-13</Dt></Dt></Bal>
              <Bal><Tp><CdOrPrtry><Cd>ITBD</Cd></CdOrPrtry></Tp><Amt Ccy="EUR">20.00</Amt><CdtDbtInd>DBIT</CdtDbtInd><Dt><Dt>2026-11-16</Dt></Dt></Bal>
              <Ntry>
                <Amt Ccy="EUR">120.00</Amt><CdtDbtInd>DBIT</CdtDbtInd><Sts><Cd>BOOK</Cd></Sts>
                <BookgDt><Dt>2026-11-16</Dt></BookgDt><ValDt><Dt>2026-11-16</Dt></ValDt>
                <AcctSvcrRef>A1</AcctSvcrRef>
                <BkTxCd><Prtry><Cd>NMSC+105+9310</Cd><Issr>DK</Issr></Prtry></BkTxCd>
                <NtryDtls><TxDtls>
                  <Refs><EndToEndId>E2E-1</EndToEndId><MndtId>MANDAT-7</MndtId></Refs>
                  <RltdPties>
                    <Cdtr><Pty><Nm>PayPal Europe</Nm></Pty></Cdtr>
                    <CdtrAcct><Id><IBAN>LU89751000135104200E</IBAN></Id></CdtrAcct>
                    <UltmtCdtr><Pty><Nm>Buchhandlung am Markt</Nm></Pty></UltmtCdtr>
                  </RltdPties>
                  <RltdAgts><CdtrAgt><FinInstnId><BICFI>PPLXLUL2</BICFI></FinInstnId></CdtrAgt></RltdAgts>
                  <RmtInf><Ustrd>1040 Bestellung </Ustrd><Ustrd>4711</Ustrd></RmtInf>
                </TxDtls></NtryDtls>
                <AddtlNtryInf>Lastschrift</AddtlNtryInf>
              </Ntry>
              <Ntry>
                <Amt Ccy="EUR">5.00</Amt><CdtDbtInd>DBIT</CdtDbtInd><Sts><Cd>PDNG</Cd></Sts>
                <BookgDt><Dt>2026-11-17</Dt></BookgDt><ValDt><Dt>2026-11-17</Dt></ValDt>
                <AddtlNtryInf>Kontoführung</AddtlNtryInf>
              </Ntry>
            </Rpt>
          </BkToCstmrAcctRpt>
        </Document>
        """;

    private static libfintx.FinTS.Camt.CamtStatement Parse(string report = Report)
    {
        var parser = new Camt052Parser();
        parser.ProcessDocument(report, Encoding.UTF8);
        return Assert.Single(parser.statements);
    }

    [Fact]
    public void A_report_without_booked_balance_has_no_end_balance()
    {
        const string interim = """<Bal><Tp><CdOrPrtry><Cd>ITBD</Cd></CdOrPrtry></Tp><Amt Ccy="EUR">20.00</Amt><CdtDbtInd>DBIT</CdtDbtInd><Dt><Dt>2026-11-16</Dt></Dt></Bal>""";
        Assert.Contains(interim, Report);

        var statement = Parse(Report.Replace(interim, string.Empty));
        Assert.False(statement.HasEndBalance);
        Assert.Null(statement.EndBalance);
    }

    [Fact]
    public void Reads_the_account_and_takes_the_interim_balance_as_end_balance()
    {
        var statement = Parse();

        Assert.Equal("DE02430609670000000001", statement.AccountCode);
        Assert.Equal(100.00m, statement.StartBalance);
        Assert.True(statement.HasEndBalance);
        Assert.Equal(-20.00m, statement.EndBalance);
        Assert.Equal(new System.DateTime(2026, 11, 16), statement.EndDate);
    }

    [Fact]
    public void Reads_the_counterpart_the_ultimate_creditor_and_the_references()
    {
        var transaction = Parse().Transactions[0];

        Assert.False(transaction.Pending);
        Assert.Equal(-120.00m, transaction.Amount);
        Assert.Equal("PayPal Europe", transaction.PartnerName);
        Assert.Equal("Buchhandlung am Markt", transaction.UltimatePartnerName);
        Assert.Equal("LU89751000135104200E", transaction.AccountCode);
        Assert.Equal("PPLXLUL2", transaction.BankCode);
        Assert.Equal("1040 Bestellung4711", transaction.Description);
        Assert.Equal("E2E-1", transaction.EndToEndId);
        Assert.Equal("MANDAT-7", transaction.MandateId);
        Assert.Equal("105", transaction.TypeCode);
        Assert.Equal("Lastschrift", transaction.Text);
    }

    [Fact]
    public void Reads_a_pending_entry_without_transaction_details()
    {
        var transaction = Parse().Transactions.Last();

        Assert.True(transaction.Pending);
        Assert.Equal(-5.00m, transaction.Amount);
        Assert.Equal("Kontoführung", transaction.Text);
        Assert.Null(transaction.PartnerName);
    }
}
