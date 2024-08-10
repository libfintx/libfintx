using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using libfintx.FinTS.Camt;
using libfintx.FinTS.Data.Segment;
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS;

public partial class FinTsClient
{
    /// <summary>
    /// Regex pattern for HIRMG/HIRMS messages.
    /// </summary>
    private const string PatternResultMessage = @"(\d{4}):.*?:(.+)";

    private Segment Parse_Segment(string segmentCode)
    {
        Segment segment = null;
        try
        {
            segment = SegmentParserFactory.ParseSegment(segmentCode);
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"Couldn't parse segment: {ex.Message}{Environment.NewLine}{segmentCode}");
        }
        return segment;
    }

    /// <summary>
    /// Parsing segment -> UPD, BPD
    /// </summary>
    /// <param name="Message"></param>
    /// <returns></returns>
    internal List<HBCIBankMessage> Parse_Segments(string Message)
    {
        Logger.LogInformation("Parsing segments ...");

        try
        {
            List<HBCIBankMessage> result = new List<HBCIBankMessage>();

            List<string> rawSegments = Helper.SplitEncryptedSegments(Message);

            List<Segment> segments = new List<Segment>();
            foreach (var item in rawSegments)
            {
                Segment segment = Parse_Segment(item);
                if (segment != null)
                    segments.Add(segment);
            }

            // BPD
            string rawBpd = string.Empty;
            var bpaMatch = Regex.Match(Message, @"(HIBPA.+?)\b(HITAN|HNHBS|HISYN|HIUPA)\b");
            if (bpaMatch.Success)
                rawBpd = bpaMatch.Groups[1].Value;
            if (rawBpd.Length > 0)
            {
                if (rawBpd.EndsWith("''"))
                    rawBpd = rawBpd.Substring(0, rawBpd.Length - 1);

                this.BdpStore.SaveBPD(280, ConnectionDetails.Blz, rawBpd)
                    .Wait();
                this.BPD = BankParameterData.BPD.Parse(rawBpd, Logger);
            }

            // UPD
            string upd = string.Empty;
            var upaMatch = Regex.Match(Message, @"(HIUPA.+?)\b(HITAN|HNHBS|HIKIM)\b");
            if (upaMatch.Success)
                upd = upaMatch.Groups[1].Value;
            if (upd.Length > 0)
            {
                Logger.LogInformation("Saving UPD ...");
                Helper.SaveUPD(ConnectionDetails.Blz, ConnectionDetails.UserId, upd);
                UPD.ParseUpd(upd, Logger);
            }

            if (UPD.AccountList != null)
            {
                //Add BIC to Account information (Not retrieved bz UPD??)
                foreach (AccountInformation accInfo in UPD.AccountList)
                    accInfo.AccountBic = ConnectionDetails.Bic;
            }

            foreach (var segment in segments)
            {
                if (segment.Name == "HIRMG")
                {
                    // HIRMG:2:2+9050::Die Nachricht enthÃ¤lt Fehler.+9800::Dialog abgebrochen+9010::Initialisierung fehlgeschlagen, Auftrag nicht bearbeitet.
                    // HIRMG:2:2+9800::Dialogabbruch.

                    string[] HIRMG_messages = segment.Payload.Split('+');
                    foreach (var HIRMG_message in HIRMG_messages)
                    {
                        var message = Parse_BankCode_Message(HIRMG_message);
                        if (message != null)
                            result.Add(message);
                    }
                }

                if (segment.Name == "HIRMS")
                {
                    // HIRMS:3:2:2+9942::PIN falsch. Zugang gesperrt.'
                    string[] HIRMS_messages = segment.Payload.Split('+');
                    foreach (var HIRMS_message in HIRMS_messages)
                    {
                        var message = Parse_BankCode_Message(HIRMS_message);
                        if (message != null)
                            result.Add(message);
                    }

                    var securityMessage = result.FirstOrDefault(m => m.Code == "3920");
                    if (securityMessage != null)
                    {
                        string message = securityMessage.Message;

                        string TAN = string.Empty;
                        string TANf = string.Empty;

                        string[] procedures = Regex.Split(message, @"\D+");

                        foreach (string value in procedures)
                        {
                            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int i))
                            {
                                if (value.StartsWith("9"))
                                {
                                    if (string.IsNullOrEmpty(TAN))
                                        TAN = i.ToString();

                                    if (string.IsNullOrEmpty(TANf))
                                        TANf = i.ToString();
                                    else
                                        TANf += $";{i}";
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(this.HIRMS))
                        {
                            this.HIRMS = TAN;
                        }
                        else
                        {
                            if (!TANf.Contains(this.HIRMS))
                                throw new Exception($"Invalid HIRMS/Tan-Mode {this.HIRMS} detected. Please choose one of the allowed modes: {TANf}");
                        }
                        this.HIRMSf = TANf;

                        // Parsing TAN processes
                        if (!string.IsNullOrEmpty(this.HIRMS))
                            Parse_TANProcesses(rawBpd);

                    }
                }

                if (segment.Name == "HNHBK")
                {
                    if (segment.DataElements.Count < 3)
                        throw new InvalidOperationException($"Expected segment '{segment}' to contain at least 3 data elements in payload.");

                    var dialogId = segment.DataElements[2];
                    this.HNHBK = dialogId;
                }

                if (segment.Name == "HISYN")
                {
                    this.SystemId = segment.Payload;
                    Logger.LogInformation("Customer System ID: " + this.SystemId);
                }

                if (segment.Name == "HNHBS")
                {
                    if (segment.Payload == null || segment.Payload == "0")
                        this.HNHBS = 2;
                    else
                        this.HNHBS = Convert.ToInt32(segment.Payload) + 1;
                }

                if (segment.Name == "HISALS")
                {
                    if (this.HISALS < segment.Version)
                        this.HISALS = segment.Version;
                }

                if (segment.Name == "HITANS")
                {
                    var hitans = (HITANS) segment;
                    if (this.HIRMS == null)
                    {
                        // Die höchste HKTAN-Version auswählen, welche in den erlaubten TAN-Verfahren (3920) enthalten ist.
                        var tanProcessesHirms = this.HIRMSf.Split(';').Select(tp => Convert.ToInt32(tp));
                        if (hitans.TanProcesses.Select(tp => tp.TanCode).Intersect(tanProcessesHirms).Any())
                            this.HITANS = segment.Version;
                    }
                    else
                    {
                        if (hitans.TanProcesses.Any(tp => tp.TanCode == Convert.ToInt32(this.HIRMS)))
                            this.HITANS = segment.Version;
                    }
                }

                if (segment.Name == "HITAN")
                {
                    // HITAN:5:7:3+S++8578-06-23-13.22.43.709351
                    // HITAN:5:7:4+4++8578-06-23-13.22.43.709351+Bitte Auftrag in Ihrer App freigeben.
                    if (segment.DataElements.Count < 3)
                        throw new InvalidOperationException($"Invalid HITAN segment '{segment}'. Payload must have at least 3 data elements.");
                    this.HITAN = segment.DataElements[2];
                }

                if (segment.Name == "HIKAZS")
                {
                    if (this.HIKAZS == 0)
                    {
                        this.HIKAZS = segment.Version;
                    }
                    else
                    {
                        if (segment.Version > this.HIKAZS)
                            this.HIKAZS = segment.Version;
                    }
                }

                if (segment.Name == "HICAZS")
                {
                    if (segment.Payload.Contains("camt.052.001.02"))
                        this.HICAZS_Camt = CamtScheme.Camt052_001_02;
                    else if (segment.Payload.Contains("camt.052.001.08"))
                        this.HICAZS_Camt = CamtScheme.Camt052_001_08;
                    else // Fallback
                        this.HICAZS_Camt = CamtScheme.Camt052_001_02;
                }

                if (segment.Name == "HISPAS")
                {
                    var hispas = segment as HISPAS;
                    if (this.HISPAS < segment.Version)
                    {
                        this.HISPAS = segment.Version;

                        if (hispas.Payload.Contains("pain.001.001.03"))
                            this.HISPAS_Pain = 1;
                        else if (hispas.Payload.Contains("pain.001.002.03"))
                            this.HISPAS_Pain = 2;
                        else if (hispas.Payload.Contains("pain.001.003.03"))
                            this.HISPAS_Pain = 3;

                        if (this.HISPAS_Pain == 0)
                            this.HISPAS_Pain = 3; // -> Fallback. Most banks accept the newest pain version

                        this.HISPAS_AccountNationalAllowed = hispas.IsAccountNationalAllowed;
                    }
                }
            }

            // Fallback if HIKAZS is not delivered by BPD (eg. Postbank)
            if (this.HIKAZS == 0)
                this.HIKAZS = 0;

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogInformation(ex.ToString());

            throw new InvalidOperationException($"Software error: {ex.Message}", ex);
        }
    }

    internal List<Segment> Parse_Message(string message)
    {
        List<string> values = Helper.SplitEncryptedSegments(message);

        List<Segment> segments = new List<Segment>();
        foreach (var item in values)
        {
            Segment segment = Parse_Segment(item);
            if (segment != null)
                segments.Add(segment);
        }

        foreach (var segment in segments)
        {
            if (segment.Name == "HNHBS")
            {
                if (segment.Payload == null || segment.Payload == "0")
                    this.HNHBS = 2;
                else
                    this.HNHBS = Convert.ToInt32(segment.Payload) + 1;
            }

            if (segment.Name == "HITAN")
            {
                // HITAN:5:7:3+S++8578-06-23-13.22.43.709351
                // HITAN:5:7:4+4++8578-06-23-13.22.43.709351+Bitte Auftrag in Ihrer App freigeben.
                // HITAN:5:6:4+4++76ma3j/MKH0BAABsRcJNhG?+owAQA+Eine neue TAN steht zur Abholung bereit.  Die TAN wurde reserviert am  16.11.2021 um 13?:54?:59 Uhr. Eine Push-Nachricht wurde versandt.  Bitte geben Sie die TAN ein.'
                if (segment.DataElements.Count < 3)
                    throw new InvalidOperationException($"Invalid HITAN segment '{segment}'. Payload must have at least 3 data elements.");
                this.HITAN = segment.DataElements[2];
            }
        }

        return segments;
    }

    internal AccountBalance Parse_Balance(string message)
    {
        var hirms = message.Substring(message.IndexOf("HIRMS") + 5);
        hirms = hirms.Substring(0, (hirms.Contains("'") ? hirms.IndexOf('\'') : hirms.Length));
        var hirmsParts = hirms.Split(':');

        AccountBalance balance = new AccountBalance();
        balance.Message = hirmsParts[hirmsParts.Length - 1];

        if (message.Contains("+0020::"))
        {
            var hisal = message.Substring(message.IndexOf("HISAL") + 5);
            hisal = hisal.Substring(0, (hisal.Contains("'") ? hisal.IndexOf('\'') : hisal.Length));
            var hisalParts = hisal.Split('+');

            balance.Successful = true;

            var hisalAccountParts = hisalParts[1].Split(':');
            if (hisalAccountParts.Length == 4)
            {
                balance.AccountType = new AccountInformation()
                {
                    AccountNumber = hisalAccountParts[0],
                    AccountBankCode = hisalAccountParts.Length > 3 ? hisalAccountParts[3] : null,
                    AccountType = hisalParts[2],
                    AccountCurrency = hisalParts[3],
                    AccountBic = !string.IsNullOrEmpty(hisalAccountParts[1]) ? hisalAccountParts[1] : null
                };
            }
            else if (hisalAccountParts.Length == 2)
            {
                balance.AccountType = new AccountInformation()
                {
                    AccountIban = hisalAccountParts[0],
                    AccountBic = hisalAccountParts[1]
                };
            }

            var hisalBalanceParts = hisalParts[4].Split(':');
            if (hisalBalanceParts[1].IndexOf("e-9", StringComparison.OrdinalIgnoreCase) >= 0)
                balance.Balance = 0; // Deutsche Bank liefert manchmal "E-9", wenn der Kontostand 0 ist. Siehe Test_Parse_Balance und https://homebanking-hilfe.de/forum/topic.php?t=24155
            else
                balance.Balance = Convert.ToDecimal($"{(hisalBalanceParts[0] == "D" ? "-" : "")}{hisalBalanceParts[1]}");


            //from here on optional fields / see page 46 in "FinTS_3.0_Messages_Geschaeftsvorfaelle_2015-08-07_final_version.pdf"
            if (hisalParts.Length > 5 && hisalParts[5].Contains(":"))
            {
                var hisalMarkedBalanceParts = hisalParts[5].Split(':');
                balance.MarkedTransactions = Convert.ToDecimal($"{(hisalMarkedBalanceParts[0] == "D" ? "-" : "")}{hisalMarkedBalanceParts[1]}");
            }

            if (hisalParts.Length > 6 && hisalParts[6].Contains(":"))
            {
                balance.CreditLine = Convert.ToDecimal(hisalParts[6].Split(':')[0].TrimEnd(','));
            }

            if (hisalParts.Length > 7 && hisalParts[7].Contains(":"))
            {
                balance.AvailableBalance = Convert.ToDecimal(hisalParts[7].Split(':')[0].TrimEnd(','));
            }

            /* ---------------------------------------------------------------------------------------------------------
             * In addition to the above fields, the following fields from HISAL could also be implemented:
             * 
             * - 9/Bereits verfügter Betrag
             * - 10/Überziehung
             * - 11/Buchungszeitpunkt
             * - 12/Fälligkeit 
             * 
             * Unfortunately I'm missing test samples. So I drop support unless we get test messages for this fields.
             ------------------------------------------------------------------------------------------------------------ */
        }
        else
        {
            balance.Successful = false;

            string msg = string.Empty;
            for (int i = 1; i < hirmsParts.Length; i++)
            {
                msg = msg + "??" + hirmsParts[i].Replace("::", ": ");
            }
            Logger.LogInformation(msg);
        }

        return balance;
    }

    internal static string Parse_Transactions_Startpoint(string bankCode)
    {
        return Regex.Match(bankCode, @"\+3040::[^:]+:(?<startpoint>[^'\+:]+)['\+:]").Groups["startpoint"].Value;
    }

    /// <summary>
    /// Parse tan processes
    /// </summary>
    /// <returns></returns>
    private bool Parse_TANProcesses(string bpd)
    {
        try
        {
            List<TanProcess> list = new List<TanProcess>();

            string[] processes = this.HIRMSf.Split(';');

            // Examples from bpd

            // 944:2:SECUREGO:
            // 920:2:smsTAN:
            // 920:2:BestSign:

            foreach (var process in processes)
            {
                string pattern = process + ":.*?:.*?:(?'name'.*?):.*?:(?'name2'.*?):";

                Regex rgx = new Regex(pattern);

                foreach (Match match in rgx.Matches(bpd))
                {
                    int i = 0;

                    if (!process.Equals("999")) // -> PIN/TAN step 1
                    {
                        if (int.TryParse(match.Groups["name2"].Value, out i))
                            list.Add(new TanProcess { ProcessNumber = process, ProcessName = match.Groups["name"].Value });
                        else
                            list.Add(new TanProcess { ProcessNumber = process, ProcessName = match.Groups["name2"].Value });
                    }
                }
            }

            TanProcesses.Items = list;

            return true;
        }
        catch { return false; }
    }

    internal static IEnumerable<string> Parse_TANMedium(string bankCode)
    {
        // HITAB:5:4:3+0+A:1:::::::::::Handy::::::::+A:2:::::::::::iPhone Abid::::::::
        // HITAB:4:4:3+0+M:1:::::::::::mT?:MFN1:********0340'
        // HITAB:5:4:3+0+M:2:::::::::::Unregistriert 1::01514/654321::::::+M:1:::::::::::Handy:*********4321:::::::
        // HITAB:4:4:3+0+M:1:::::::::::mT?:MFN1:********0340+G:1:SO?:iPhone:00:::::::::SO?:iPhone''

        // For easier matching, replace '?:' by some special character
        bankCode = bankCode.Replace("?:", @"\");

        foreach (Match match in Regex.Matches(bankCode, @"\+[AGMS]:[012]:(?<Kartennummer>[^:]*):(?<Kartenfolgenummer>[^:]*):+(?<Bezeichnung>[^+:]+)"))
        {
            yield return match.Groups["Bezeichnung"].Value.Replace(@"\", "?:");
        }
    }

    /// <summary>
    /// Parse a single bank result message.
    /// </summary>
    /// <param name="bankCodeMessage"></param>
    /// <returns></returns>
    internal static HBCIBankMessage? Parse_BankCode_Message(string bankCodeMessage)
    {
        var match = Regex.Match(bankCodeMessage, PatternResultMessage);
        if (match.Success)
        {
            var code = match.Groups[1].Value;
            var message = match.Groups[2].Value;

            message = message.Replace("?:", ":");
            message = message.Replace("?'", "'");
            message = message.Replace("?+", "+");

            return new HBCIBankMessage(code, message);
        }
        return null;
    }

    /// <summary>
    /// Parse bank error codes
    /// </summary>
    /// <param name="bankCode"></param>
    /// <returns>Banks messages with "??" as seperator.</returns>
    internal IEnumerable<HBCIBankMessage> Parse_BankCode(string bankCode)
    {
        var rawSegments = Helper.SplitEncryptedSegments(bankCode);
        var segments = new List<Segment>();
        foreach (var item in rawSegments)
        {
            Segment segment = Parse_Segment(item);
            if (segment != null)
                segments.Add(segment);
        }

        foreach (var segment in segments)
        {
            if (segment.Name == "HIRMG" || segment.Name == "HIRMS")
            {
                // HIRMS:4:2:3+9210::*?'Ausführung bis?' muss nach ?'Ausführung ab?' liegen.+9210::*Die BIC wurde angepasst.+0900::Freigabe erfolgreich
                var messages = segment.DataElements;
                foreach (var HIRMG_message in messages)
                {
                    var message = Parse_BankCode_Message(HIRMG_message);
                    if (message != null)
                        yield return message;
                }
            }
        }
    }
}
