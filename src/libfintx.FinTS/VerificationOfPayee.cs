/*
 *
 *  This file is part of libfintx.
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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using libfintx.FinTS.Data;

namespace libfintx.FinTS
{
    /// <summary>Result of the verification of payee for one credit transfer (pain.002 status RCVC, RVMC, RVNM, RVNA, PDNG).</summary>
    public enum VerificationOfPayeeMatch
    {
        /// <summary>The name matches the account (RCVC).</summary>
        Match,

        /// <summary>The name almost matches (RVMC); the bank reports the name of the account holder.</summary>
        CloseMatch,

        /// <summary>The name does not match (RVNM): risk of paying the wrong person.</summary>
        NoMatch,

        /// <summary>The check was not possible (RVNA), see the reason code.</summary>
        NotApplicable,

        /// <summary>The answer of the payee's bank is still pending (PDNG).</summary>
        Pending,

        /// <summary>The bank reported no result.</summary>
        Unknown,
    }

    /// <summary>Verification of payee of one credit transfer.</summary>
    public class VerificationOfPayeeItem
    {
        public VerificationOfPayeeItem(string name, string iban, VerificationOfPayeeMatch result, string nameAtBank,
            string reasonCode, string information)
        {
            Name = name;
            Iban = iban;
            Result = result;
            NameAtBank = nameAtBank;
            ReasonCode = reasonCode;
            Information = information;
        }

        /// <summary>Name of the payee as submitted.</summary>
        public string Name { get; }

        public string Iban { get; }

        public VerificationOfPayeeMatch Result { get; }

        /// <summary>
        /// For <see cref="VerificationOfPayeeMatch.CloseMatch"/> the name the payee's bank holds; it may be taken over
        /// into the own master data.
        /// </summary>
        public string NameAtBank { get; }

        /// <summary>
        /// For <see cref="VerificationOfPayeeMatch.NotApplicable"/>: AB11 (the payee's bank did not answer in time) or
        /// AG03 (not supported, e.g. no payment account or the bank does not take part).
        /// </summary>
        public string ReasonCode { get; }

        /// <summary>Additional information of the bank.</summary>
        public string Information { get; }
    }

    /// <summary>
    /// Verification of payee before an order is released. Show the items and the <see cref="Explanation"/> to the
    /// user; when <see cref="ConfirmationRequired"/>, execute only after the user explicitly confirms.
    /// </summary>
    public class VerificationOfPayeeResult
    {
        public VerificationOfPayeeResult(IReadOnlyList<VerificationOfPayeeItem> items, string explanation,
            bool explanationStructured, bool confirmationRequired)
        {
            Items = items;
            Explanation = explanation;
            ExplanationStructured = explanationStructured;
            ConfirmationRequired = confirmationRequired;
        }

        public IReadOnlyList<VerificationOfPayeeItem> Items { get; }

        /// <summary>Text of the bank about the consequences of releasing the order despite a deviation (must be shown).</summary>
        public string Explanation { get; }

        /// <summary>
        /// The explanation may contain &lt;br&gt;, &lt;p&gt;, &lt;b&gt;, &lt;i&gt;, &lt;u&gt;, &lt;ul&gt;, &lt;ol&gt;,
        /// &lt;li&gt; and &lt;a href&gt; (VOP 1.02, data dictionary "Aufklärungstext").
        /// </summary>
        public bool ExplanationStructured { get; }

        /// <summary>The explanation without formatting.</summary>
        public string ExplanationPlainText => Explanation == null ? null : VerificationOfPayee.PlainText(Explanation, ExplanationStructured);

        /// <summary>The order must only be released after the user confirms it explicitly.</summary>
        public bool ConfirmationRequired { get; internal set; }

        public bool AllMatch => Items.Count > 0 && Items.All(i => i.Result == VerificationOfPayeeMatch.Match);
    }

    /// <summary>Answer of the bank to HKVPP (HIVPP).</summary>
    internal sealed class VerificationOfPayeeAnswer
    {
        public DataElementValue VopId { get; set; }
        public DataElementValue PollingId { get; set; }
        public byte[] PaymentStatusReport { get; set; }
        public string SingleIban { get; set; }
        public string SingleNameAtBank { get; set; }
        public string SingleOtherIdentification { get; set; }
        public string SingleResult { get; set; }
        public string SingleReason { get; set; }
        public string Explanation { get; set; }
        public int? WaitSeconds { get; set; }
    }

    /// <summary>
    /// Verification of payee after "FinTS 3.0 Messages Geschäftsvorfälle VOP" 1.02 and the payment status report
    /// pain.002.001.10 (DFÜ-Abkommen Anlage 3 V3.9, chapter 2.2.6).
    /// </summary>
    internal static class VerificationOfPayee
    {
        /// <summary>HIVPP of an answer; <c>null</c> when it has none.</summary>
        public static VerificationOfPayeeAnswer ParseAnswer(string message)
        {
            var segment = Helper.SplitEncryptedSegments(message).FirstOrDefault(s => s.StartsWith("HIVPP:"));
            if (segment == null)
                return null;

            // 1 VOP id, 2 VOP id valid until, 3 polling id, 4 report descriptor, 5 payment status report, 6 single result
            // (IBAN : IBAN addition : name at bank : other identification : result : reason RVNA), 7 explanation, 8 wait time
            var e = Helper.SplitSegmentValues(segment);
            DataElementValue Value(int element, int part = 0) =>
                element < e.Count && part < e[element].Count && e[element][part].Text.Length > 0 ? e[element][part] : null;

            return new VerificationOfPayeeAnswer
            {
                VopId = Value(1),
                PollingId = Value(3),
                PaymentStatusReport = Value(5) == null ? null : Encoding.GetEncoding("ISO-8859-1").GetBytes(Value(5).Text),
                SingleIban = Value(6, 0)?.Text,
                SingleNameAtBank = Value(6, 2)?.Text,
                SingleOtherIdentification = Value(6, 3)?.Text,
                SingleResult = Value(6, 4)?.Text,
                SingleReason = Value(6, 5)?.Text,
                Explanation = Value(7)?.Text,
                WaitSeconds = int.TryParse(Value(8)?.Text, out var wait) ? wait : (int?) null,
            };
        }

        /// <summary>One transaction of a payment status report.</summary>
        internal sealed class ReportItem
        {
            public string EndToEndId;
            public string Iban;
            public string Status;
            public string NameAtBank;
            public string Reason;
            public string Information;
        }

        /// <summary>
        /// Reads one or more (possibly split) payment status reports; later reports overwrite earlier ones. Also the
        /// information on group and payment level: each AddtlInf there starts with the status it refers to (chapter
        /// 2.2.6.4/2.2.6.5).
        /// </summary>
        public static (List<ReportItem> Items, Dictionary<string, string> Information) ReadReports(IEnumerable<byte[]> parts)
        {
            var result = new Dictionary<string, ReportItem>();
            var information = new Dictionary<string, List<string>>();
            var buffer = new List<byte>();
            foreach (var part in parts)
            {
                buffer.AddRange(part);
                XDocument document;
                try
                {
                    using var stream = new MemoryStream(buffer.ToArray());
                    document = XDocument.Load(XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }));
                }
                catch (XmlException)
                {
                    continue; // part of a report, put together with the next one
                }
                buffer.Clear();

                foreach (var level in document.Descendants().Where(x => x.Name.LocalName == "OrgnlGrpInfAndSts" || x.Name.LocalName == "OrgnlPmtInfAndSts"))
                {
                    foreach (var info in level.Elements().Where(x => x.Name.LocalName == "StsRsnInf").SelectMany(x => x.Elements().Where(y => y.Name.LocalName == "AddtlInf")))
                    {
                        var text = info.Value.Trim();
                        if (text.IndexOf(' ') != 4)
                            continue;

                        var code = text.Substring(0, 4).ToUpperInvariant();
                        if (!information.TryGetValue(code, out var lines))
                            information[code] = lines = new List<string>();
                        var line = text.Substring(5).Trim();
                        if (!lines.Contains(line))
                            lines.Add(line);
                    }
                }

                // Step by step delivery (data dictionary "Art der Lieferung"): later reports only name transactions that
                // were PDNG and are not RCVC now, so a PDNG transaction not named any more is a match.
                var named = new HashSet<string>(document.Descendants().Where(x => x.Name.LocalName == "TxInfAndSts")
                    .Select(tx => Child(tx, "OrgnlEndToEndId")?.Value.Trim()).Where(id => id != null));
                foreach (var pending in result.Where(r => r.Value.Status == "PDNG" && !named.Contains(r.Key)).Select(r => r.Key).ToList())
                    result.Remove(pending);

                var number = 0;
                foreach (var tx in document.Descendants().Where(x => x.Name.LocalName == "TxInfAndSts"))
                {
                    var endToEndId = Child(tx, "OrgnlEndToEndId")?.Value.Trim();
                    var reference = Child(tx, "OrgnlTxRef");
                    var iban = reference == null ? null : Child(reference, "CdtrAcct")?.Descendants().FirstOrDefault(x => x.Name.LocalName == "IBAN")?.Value.Trim();
                    var status = Child(tx, "TxSts")?.Value.Trim().ToUpperInvariant();
                    var reasonInfo = Child(tx, "StsRsnInf");
                    var reason = reasonInfo == null ? null : Child(reasonInfo, "Rsn") is XElement rsn ? Child(rsn, "Cd")?.Value.Trim() : null;
                    // not trimmed: a long name continues in further AddtlInf in the middle of the text (Anlage 3, p. 251)
                    var infos = reasonInfo?.Elements().Where(x => x.Name.LocalName == "AddtlInf").Select(x => x.Value).Where(t => t.Trim().Length > 0).ToList()
                        ?? new List<string>();

                    string nameAtBank = null;
                    string text = null;
                    if (status == "RVMC" && infos.Count > 0)
                    {
                        // first AddtlInf is the name; longer names continue in lines starting with a quotation mark
                        var name = new StringBuilder(infos[0].TrimStart());
                        foreach (var next in infos.Skip(1).Select(f => f.TrimStart()).TakeWhile(f => "\"“”„".IndexOf(f[0]) >= 0))
                            name.Append(next.Substring(1));
                        nameAtBank = name.ToString().TrimEnd();
                    }
                    else if (infos.Count > 0)
                    {
                        text = string.Join(" ", infos.Select(i => i.Trim()));
                    }

                    result[endToEndId ?? iban ?? "#" + number] = new ReportItem
                    {
                        EndToEndId = endToEndId,
                        Iban = iban,
                        Status = status,
                        NameAtBank = nameAtBank,
                        Reason = reason,
                        Information = text,
                    };
                    number++;
                }
            }

            return (result.Values.ToList(), information.ToDictionary(i => i.Key, i => string.Join(" ", i.Value)));
        }

        private static XElement Child(XElement element, string name) => element.Elements().FirstOrDefault(x => x.Name.LocalName == name);

        /// <summary>
        /// Assigns the check result to the submitted transfers. Transactions not listed in the payment status report are a
        /// match: the result is given "only if it is not a match" (Anlage 3, chapter 2.2.6). Information on file level
        /// goes to the items with the status it refers to.
        /// </summary>
        /// <param name="payees">Submitted transfers in pain order: name, IBAN, end to end id.</param>
        /// <param name="noDeviationReported">The bank reported 0025 ("no deviation"), so without a result all match.</param>
        public static VerificationOfPayeeResult Evaluate(VerificationOfPayeeAnswer answer, IReadOnlyList<(string Name, string Iban, string EndToEndId)> payees,
            IReadOnlyList<byte[]> reports, bool explanationStructured, bool confirmationRequired, bool noDeviationReported)
        {
            var items = new List<VerificationOfPayeeItem>();
            if (answer.SingleResult != null && payees.Count == 1)
            {
                var p = payees[0];
                items.Add(new VerificationOfPayeeItem(p.Name, p.Iban, Match(answer.SingleResult), answer.SingleNameAtBank,
                    answer.SingleReason, answer.SingleOtherIdentification));
            }
            else if (reports.Count > 0)
            {
                var (report, information) = ReadReports(reports);
                foreach (var p in payees)
                {
                    var iban = (p.Iban ?? string.Empty).Replace(" ", string.Empty);
                    var r = report.FirstOrDefault(x => x.EndToEndId != null && x.EndToEndId == p.EndToEndId)
                        ?? report.FirstOrDefault(x => string.Equals(x.Iban, iban, StringComparison.OrdinalIgnoreCase));
                    if (r == null)
                    {
                        items.Add(new VerificationOfPayeeItem(p.Name, p.Iban, VerificationOfPayeeMatch.Match, null, null,
                            information.TryGetValue("RCVC", out var matched) ? matched : null));
                        continue;
                    }

                    var texts = new[] { r.Information, r.Status != null && information.TryGetValue(r.Status, out var level) ? level : null }
                        .Select(t => t?.Trim().TrimStart('-', ' ')).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                    items.Add(new VerificationOfPayeeItem(p.Name, p.Iban, Match(r.Status), r.NameAtBank, r.Reason,
                        texts.Count > 0 ? string.Join(" ", texts) : null));
                }
            }
            else
            {
                var result = noDeviationReported ? VerificationOfPayeeMatch.Match : VerificationOfPayeeMatch.Unknown;
                items.AddRange(payees.Select(p => new VerificationOfPayeeItem(p.Name, p.Iban, result, null, null, null)));
            }

            return new VerificationOfPayeeResult(items, answer.Explanation, explanationStructured, confirmationRequired);
        }

        public static VerificationOfPayeeMatch Match(string status)
        {
            switch (status?.Trim().ToUpperInvariant())
            {
                case "RCVC": return VerificationOfPayeeMatch.Match;
                case "RVMC": return VerificationOfPayeeMatch.CloseMatch;
                case "RVNM": return VerificationOfPayeeMatch.NoMatch;
                case "RVNA": return VerificationOfPayeeMatch.NotApplicable;
                case "PDNG": return VerificationOfPayeeMatch.Pending;
                default: return VerificationOfPayeeMatch.Unknown;
            }
        }

        /// <summary>Formatted explanation (&lt;br&gt;, &lt;p&gt;, &lt;li&gt; ...) as plain text.</summary>
        public static string PlainText(string text, bool structured)
        {
            if (!structured)
                return text.Trim();

            var t = Regex.Replace(text, @"<\s*(br|p|/p|ul|/ul|ol|/ol)\s*/?\s*>", "\n", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"<\s*li\s*>", "\n• ", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"<\s*a\s+href\s*=\s*""([^""]*)""\s*>(.*?)<\s*/\s*a\s*>", "$2 ($1)", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"<[^>]+>", string.Empty);
            return string.Join("\n", t.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
        }
    }
}
