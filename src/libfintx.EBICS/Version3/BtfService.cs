using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace libfintx.EBICS.Version3
{
    /// <summary>A bank-advertised BTF service. Codes are extensible and not restricted to German payments.</summary>
    public sealed class BtfService
    {
        public string ServiceName { get; set; }
        public string Scope { get; set; }
        public string ServiceOption { get; set; }
        public string ContainerType { get; set; }
        public string MessageName { get; set; }
        public string MessageVariant { get; set; }
        public string MessageVersion { get; set; }
        public string MessageFormat { get; set; }

        public XElement ToXml()
        {
            if (string.IsNullOrWhiteSpace(ServiceName) || string.IsNullOrWhiteSpace(MessageName))
                throw new ArgumentException("ServiceName and MessageName are required.");
            XNamespace n = Ebics3Client.Namespace;
            var message = new XElement(n + "MsgName", MessageName);
            Attribute(message, "variant", MessageVariant);
            Attribute(message, "version", MessageVersion);
            Attribute(message, "format", MessageFormat);
            return new XElement(n + "Service", new XElement(n + "ServiceName", ServiceName),
                Scope == null ? null : new XElement(n + "Scope", Scope),
                ServiceOption == null ? null : new XElement(n + "ServiceOption", ServiceOption),
                ContainerType == null ? null : new XElement(n + "Container", new XAttribute("containerType", ContainerType)), message);
        }

        internal static void Attribute(XElement e, string name, string value)
        {
            if (value != null) e.Add(new XAttribute(name, value));
        }
    }

    public sealed class BtdRequest
    {
        public BtfService Service { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        /// <summary>Persist/validate the decoded payload before sending a positive receipt.
        /// Throwing sends a best-effort negative receipt. Keep this operation idempotent by transaction/business ID.</summary>
        public Func<Ebics3Download, CancellationToken, Task> BeforeReceipt { get; set; }
        public IList<XElement> Parameters { get; } = new List<XElement>();
        internal XElement ToXml()
        {
            if (Service == null) throw new ArgumentNullException(nameof(Service));
            if (StartDate.HasValue != EndDate.HasValue || StartDate > EndDate)
                throw new ArgumentException("Supply both dates in chronological order, or neither.");
            XNamespace n = Ebics3Client.Namespace;
            return new XElement(n + "BTDOrderParams", Service.ToXml(), StartDate.HasValue
                ? new XElement(n + "DateRange", new XElement(n + "Start", StartDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    new XElement(n + "End", EndDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))) : null, Parameters);
        }
    }

    public sealed class BtuRequest
    {
        public BtfService Service { get; set; }
        public byte[] Data { get; set; }
        public string FileName { get; set; }
        /// <summary>Authorise within EBICS. False means external authorisation, not an unsigned transport.</summary>
        public bool ElectronicSignature { get; set; } = true;
        public bool RequestDistributedSignature { get; set; }
        /// <summary>Optional complete set of signers for this upload (e.g. French TS double signature).
        /// Empty uses Config.User. Each signer must belong to the submitting partner; bank policy determines required classes/count.</summary>
        public IList<EBICSConfig.UserParams> Signers { get; } = new List<EBICSConfig.UserParams>();
        public IList<XElement> Parameters { get; } = new List<XElement>();
        internal XElement ToXml()
        {
            if (Service == null) throw new ArgumentNullException(nameof(Service));
            if (Data == null) throw new ArgumentNullException(nameof(Data));
            if (RequestDistributedSignature && !ElectronicSignature)
                throw new ArgumentException("Distributed signatures require ElectronicSignature.");
            XNamespace n = Ebics3Client.Namespace;
            var flag = ElectronicSignature ? new XElement(n + "SignatureFlag") : null;
            if (RequestDistributedSignature) flag.Add(new XAttribute("requestEDS", true));
            var result = new XElement(n + "BTUOrderParams", Service.ToXml(), flag, Parameters);
            BtfService.Attribute(result, "fileName", FileName);
            return result;
        }
    }

    public sealed class Ebics3Download
    {
        public string TransactionId { get; }
        public ReadOnlyMemory<byte> Data { get; }
        internal Ebics3Download(string transactionId, byte[] data) { TransactionId = transactionId; Data = data; }
    }

    public sealed class Ebics3Response : Responses.Response
    {
        public byte[] Data { get; internal set; } = Array.Empty<byte>();
        public string TransactionId { get; internal set; }
        public string OrderId { get; internal set; }
        public string TransactionPhase { get; internal set; }
        /// <summary>Receipt success requires EBICS_DOWNLOAD_POSTPROCESS_DONE (011000), not generic OK.</summary>
        public bool IsSuccess => BusinessReturnCode == 0 && (TransactionPhase == "Receipt"
            ? TechnicalReturnCode == 11000 : TechnicalReturnCode == 0);
        public bool ReceiptAcknowledged { get; internal set; }
    }
}
