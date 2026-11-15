using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace libfintx.EBICS.Version3
{
    public enum HacCompletion { None, Positive, Negative, LegacyNeutral }

    /// <summary>One HAC protocol step. Positive completion can also mean authorised cancellation;
    /// it is not confirmation of payment settlement (3.0.2 section 10.2.3.1).</summary>
    public sealed class HacAcknowledgement
    {
        public string Action { get; private set; }
        public HacCompletion Completion => Action switch
        {
            "ORDER_HAC_FINAL_POS" => HacCompletion.Positive,
            "ORDER_HAC_FINAL_NEG" => HacCompletion.Negative,
            "ORDER_HAC_FINAL" => HacCompletion.LegacyNeutral,
            _ => HacCompletion.None
        };
        public string ReasonCode { get; private set; }
        public IReadOnlyDictionary<string, string[]> Identifiers { get; private set; }
        public IReadOnlyList<string> AdditionalInformation { get; private set; }

        /// <summary>Read HAC pain.002.001.03 without merging identifiers from different protocol steps.</summary>
        public static IReadOnlyList<HacAcknowledgement> Parse(byte[] data, long maximumCharacters = 128 * 1024 * 1024)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            using var input = new MemoryStream(data, false);
            using var reader = XmlReader.Create(input, new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = maximumCharacters });
            var doc = XDocument.Load(reader);
            XNamespace n = "urn:iso:std:iso:20022:tech:xsd:pain.002.001.03";
            var report = doc.Root?.Element(n + "CstmrPmtStsRpt");
            if (doc.Root?.Name != n + "Document" || report == null) throw new InvalidDataException("Expected HAC pain.002.001.03.");
            var result = new List<HacAcknowledgement>();
            foreach (var step in report.Elements(n + "OrgnlPmtInfAndSts"))
            {
                var action = (string)step.Element(n + "OrgnlPmtInfId");
                var reasons = step.Elements(n + "StsRsnInf").ToArray();
                if (string.IsNullOrWhiteSpace(action) || reasons.Length != 1)
                    throw new InvalidDataException("A HAC step requires an action and exactly one status reason information group.");
                var info = reasons[0];
                var reason = (string)info.Element(n + "Rsn")?.Element(n + "Cd");
                var acknowledgement = new HacAcknowledgement { Action = action, ReasonCode = reason,
                    Identifiers = info.Element(n + "Orgtr")?.Element(n + "Id")?.Element(n + "OrgId")?.Elements(n + "Othr")
                        .Select(id => new { Name = (string)id.Element(n + "SchmeNm")?.Element(n + "Prtry"), Value = (string)id.Element(n + "Id") })
                        .Where(id => id.Name != null && id.Value != null).GroupBy(id => id.Name)
                        .ToDictionary(g => g.Key, g => g.Select(id => id.Value).ToArray()) ?? new Dictionary<string, string[]>(),
                    AdditionalInformation = info.Elements(n + "AddtlInf").Select(e => e.Value).ToArray() };
                if (acknowledgement.Completion != HacCompletion.None && info.Element(n + "Rsn") != null)
                    throw new InvalidDataException("HAC final indications must not contain a reason code.");
                if (acknowledgement.Completion == HacCompletion.None && action != "ADDITIONAL" && reason == null)
                    throw new InvalidDataException("This HAC action requires an ISO reason code.");
                if (action == "VEU_VERIFICATION_END" && reason != "DS05")
                    throw new InvalidDataException("VEU_VERIFICATION_END requires DS05 (CR EB-20-01).");
                result.Add(acknowledgement);
            }
            if (result.Count == 0) throw new InvalidDataException("HAC must contain at least one protocol step.");
            return result;
        }
    }
}
