using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using libfintx.EBICSConfig;

namespace libfintx.EBICS.Version3
{
    /// <summary>H005 transport. A client instance serialises transactions; it never retries payment uploads automatically.</summary>
    public sealed partial class Ebics3Client : IDisposable
    {
        public const string Namespace = "urn:org:ebics:H005";
        public const string SignatureNamespace = "http://www.ebics.org/S002";
        internal static readonly XNamespace N = Namespace;
        internal static readonly XNamespace S = SignatureNamespace;
        internal static readonly XNamespace D = Ebics3Crypto.Ds;
        private readonly Config config;
        private readonly Uri address;
        private readonly HttpClient http;
        private readonly bool ownsHttp;
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        public int MaximumDownloadBytes { get; set; } = 128 * 1024 * 1024;
        public int SegmentSize { get; set; } = 1024 * 1024;
        public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(100);
        public string SecurityMedium { get; set; } = "0000";
        /// <summary>Explicit migration mode for already activated EBICS 2.x user keys (CR EB-20-03/04).
        /// New enrolments and key changes still require certificates. Never enables an algorithm downgrade.</summary>
        public bool AllowMigratedUserKeys { get; set; }

        private void ValidateUserCertificate(X509Certificate2 certificate, Ebics3KeyRole role)
        {
            if (certificate != null || !AllowMigratedUserKeys) Ebics3Certificates.Validate(certificate, role);
        }

        public Ebics3Client(Config config, HttpClient httpClient = null)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            if (config.Version != EbicsVersion.H005) throw new ArgumentException("Set Version to H005.");
            if (!Uri.TryCreate(config.Address, UriKind.Absolute, out var bankAddress) || bankAddress.Scheme != "https")
                throw new ArgumentException("H005 requires an HTTPS bank address.");
            address = bankAddress;
            http = httpClient ?? new HttpClient(Ebics3Tls.CreateHandler()); ownsHttp = httpClient == null;
        }

        public Task<Ebics3Response> BTDAsync(BtdRequest request, CancellationToken cancellationToken = default) =>
            DownloadAsync("BTD", request?.ToXml() ?? throw new ArgumentNullException(nameof(request)), cancellationToken, request.BeforeReceipt);
        public Task<Ebics3Response> BTUAsync(BtuRequest request, CancellationToken cancellationToken = default) =>
            UploadAsync("BTU", request?.ToXml() ?? throw new ArgumentNullException(nameof(request)), request.Data, null, cancellationToken,
                request.Signers.Count == 0 ? null : request.Signers.ToArray());

        /// <summary>Download the customer acknowledgement (pain.002). PTK is not an H005 order.</summary>
        public Task<Ebics3Response> HACAsync(CancellationToken cancellationToken = default,
            Func<Ebics3Download, CancellationToken, Task> beforeReceipt = null) =>
            DownloadAdministrativeAsync("HAC", cancellationToken: cancellationToken, beforeReceipt: beforeReceipt);

        /// <summary>Download an administrative order. VEU parameters use the generated H005 schema types or XElement.</summary>
        public Task<Ebics3Response> DownloadAdministrativeAsync(string order, XElement parameters = null, CancellationToken cancellationToken = default,
            Func<Ebics3Download, CancellationToken, Task> beforeReceipt = null)
        {
            if (!new[] { "HAA", "HAC", "HPD", "HKD", "HTD", "HVU", "HVZ", "HVD", "HVT" }.Contains(order))
                throw new ArgumentException("Unsupported administrative download.", nameof(order));
            if (order.StartsWith("HV", StringComparison.Ordinal) && parameters == null)
                parameters = new XElement(N + (order + "OrderParams"));
            return DownloadAsync(order, parameters ?? new XElement(N + "StandardOrderParams"), cancellationToken, beforeReceipt);
        }

        private XElement InitialHeader(string order, XElement parameters, bool bankDigests = true, bool nonce = true)
        {
            var user = config.User ?? throw new ArgumentException("User configuration is required.");
            if (bankDigests)
            {
                Ebics3Certificates.Validate(config.Bank?.AuthKeys?.Certificate, Ebics3KeyRole.Authentication);
                Ebics3Certificates.Validate(config.Bank?.CryptKeys?.Certificate, Ebics3KeyRole.Encryption);
                if (config.Bank?.AuthKeys?.Version != AuthVersion.X002 || config.Bank?.CryptKeys?.Version != CryptVersion.E002)
                    throw new CryptographicException("H005 requires bank X002 and E002 keys.");
                foreach (var key in new[] { config.Bank.AuthKeys.PublicKey, config.Bank.CryptKeys.PublicKey })
                    if (key == null || key.KeySize < 2048 || key.KeySize > 16384)
                        throw new CryptographicException("Bank RSA key length must be between 2048 and 16384 bits.");
            }
            return new XElement(N + "static", new XElement(N + "HostID", user.HostId),
                nonce ? new XElement(N + "Nonce", Convert.ToHexString(RandomNumberGenerator.GetBytes(16))) : null,
                nonce ? new XElement(N + "Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)) : null,
                new XElement(N + "PartnerID", user.PartnerId), new XElement(N + "UserID", user.UserId),
                user.SystemId == null ? null : new XElement(N + "SystemID", user.SystemId),
                new XElement(N + "Product", new XAttribute("Language", "en"), config.Vendor),
                new XElement(N + "OrderDetails", new XElement(N + "AdminOrderType", order), parameters),
                bankDigests ? new XElement(N + "BankPubKeyDigests",
                    Digest("Authentication", config.Bank?.AuthKeys?.Certificate, "X002"),
                    Digest("Encryption", config.Bank?.CryptKeys?.Certificate, "E002")) : null,
                new XElement(N + "SecurityMedium", SecurityMedium));
        }
        private static XElement Digest(string element, X509Certificate2 certificate, string version) =>
            new XElement(N + element, new XAttribute("Version", version), new XAttribute("Algorithm", Ebics3Crypto.Sha256),
                Convert.ToBase64String(Ebics3Crypto.CertificateDigest(certificate)));

        private XmlDocument Request(XElement stat, XElement mutable, XElement body, string root = "ebicsRequest", bool signed = true)
        {
            var xml = new XElement(N + root, new XAttribute("Version", "H005"), new XAttribute("Revision", "1"),
                new XElement(N + "header", new XAttribute("authenticate", true), stat, mutable),
                signed ? new XElement(N + "AuthSignature") : null, body);
            var doc = Parse(xml.ToString(SaveOptions.DisableFormatting));
            if (signed)
            {
                var key = config.User?.AuthKeys;
                ValidateUserCertificate(key?.Certificate, Ebics3KeyRole.Authentication);
                if (key?.Version != AuthVersion.X002 || key.PrivateKey == null || key.PrivateKey.KeySize < 2048 || key.PrivateKey.KeySize > 16384)
                    throw new CryptographicException("An X002 authentication key of at least 2048 bits is required.");
                Ebics3Crypto.Sign(doc, key.PrivateKey);
            }
            Ebics3Schema.Validate(doc);
            return doc;
        }
        internal static XmlDocument Parse(string xml)
        {
            using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 192 * 1024 * 1024 });
            var doc = new XmlDocument { PreserveWhitespace = true, XmlResolver = null }; doc.Load(reader); return doc;
        }
        private static byte[] Payload(XElement data)
        {
            var doc = Parse(data.ToString(SaveOptions.DisableFormatting));
            Ebics3Schema.Validate(doc);
            return Encoding.UTF8.GetBytes(doc.OuterXml);
        }
        private async Task<XmlDocument> SendAsync(XmlDocument request, bool verify, CancellationToken ct)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(ResponseTimeout);
            ct = timeout.Token;
            using var content = new StringContent(request.OuterXml, Encoding.UTF8, "text/xml");
            using var message = new HttpRequestMessage(HttpMethod.Post, address) { Content = content };
            // Do not let HttpClient buffer an unbounded response before applying our size limit.
            using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var output = new MemoryStream();
            var buffer = new byte[8192]; int count;
            while ((count = await stream.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false)) != 0)
            {
                if (output.Length + count > MaximumDownloadBytes * 2L) throw new InvalidDataException("EBICS response exceeds limit.");
                output.Write(buffer, 0, count);
            }
            var doc = Parse(Encoding.UTF8.GetString(output.ToArray()));
            var expectedRoot = request.DocumentElement.LocalName == "ebicsHEVRequest" ? "ebicsHEVResponse" :
                request.DocumentElement.LocalName == "ebicsRequest" ? "ebicsResponse" : "ebicsKeyManagementResponse";
            if (doc.DocumentElement.LocalName != expectedRoot) throw new InvalidDataException("Unexpected EBICS response type.");
            Ebics3Schema.Validate(doc);
            if (verify) Ebics3Crypto.Verify(doc, config.Bank?.AuthKeys?.PublicKey);
            return doc;
        }
        private static XElement Root(XmlDocument doc) => XElement.Parse(doc.OuterXml, LoadOptions.PreserveWhitespace);
        private static XElement Static(XElement root) => root.Element(N + "header")?.Element(N + "static");
        private static XElement Mutable(XElement root) => root.Element(N + "header")?.Element(N + "mutable");
        private static XElement Transfer(XElement root) => root.Element(N + "body")?.Element(N + "DataTransfer");
        private static int Code(XElement element)
        {
            if (element == null || element.Value.Length != 6 || !int.TryParse(element.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var code))
                throw new InvalidDataException("Missing or malformed EBICS return code.");
            return code;
        }
        private static Ebics3Response Result(XElement root) => new Ebics3Response
        {
            TechnicalReturnCode = Code(Mutable(root)?.Element(N + "ReturnCode")),
            BusinessReturnCode = Code(root.Element(N + "body")?.Element(N + "ReturnCode")),
            ReportText = (string)Mutable(root)?.Element(N + "ReportText"),
            TransactionId = (string)Static(root)?.Element(N + "TransactionID"),
            OrderId = (string)Mutable(root)?.Element(N + "OrderID"),
            TransactionPhase = (string)Mutable(root)?.Element(N + "TransactionPhase")
        };
        private static void CheckTransaction(XElement root, string id, string phase)
        {
            if ((string)Static(root)?.Element(N + "TransactionID") != id || (string)Mutable(root)?.Element(N + "TransactionPhase") != phase)
                throw new InvalidDataException("EBICS response transaction or phase mismatch.");
        }
        private XmlDocument TransactionRequest(string id, string phase, XElement body, int? segment = null, bool last = false) =>
            Request(new XElement(N + "static", new XElement(N + "HostID", config.User.HostId), new XElement(N + "TransactionID", id)),
                new XElement(N + "mutable", new XElement(N + "TransactionPhase", phase), segment.HasValue
                    ? new XElement(N + "SegmentNumber", new XAttribute("lastSegment", last), segment.Value) : null), body);
        private byte[] TransactionKey(XElement info)
        {
            ValidateUserCertificate(config.User?.CryptKeys?.Certificate, Ebics3KeyRole.Encryption);
            if (config.User?.CryptKeys?.Version != CryptVersion.E002 || config.User.CryptKeys.PrivateKey == null ||
                config.User.CryptKeys.PrivateKey.KeySize < 2048 || config.User.CryptKeys.PrivateKey.KeySize > 16384)
                throw new CryptographicException("H005 requires an E002 private key between 2048 and 16384 bits.");
            var digest = info?.Element(N + "EncryptionPubKeyDigest");
            var recipient = config.User.CryptKeys;
            var expected = recipient.Certificate != null ? Ebics3Crypto.CertificateDigest(recipient.Certificate) : recipient.Digest;
            if ((string)digest?.Attribute("Version") != "E002" || (string)digest?.Attribute("Algorithm") != Ebics3Crypto.Sha256 ||
                expected == null || !CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(digest.Value), expected))
                throw new CryptographicException("Unexpected recipient encryption certificate.");
            var key = recipient.PrivateKey.Decrypt(Ebics3Crypto.NormalizeRsaValue(Convert.FromBase64String(info.Element(N + "TransactionKey").Value),
                recipient.PrivateKey.KeySize), RSAEncryptionPadding.Pkcs1);
            if (key.Length != 16) throw new CryptographicException("Invalid E002 transaction key length.");
            return key;
        }
        private async Task<Ebics3Response> DownloadAsync(string order, XElement parameters, CancellationToken ct,
            Func<Ebics3Download, CancellationToken, Task> beforeReceipt = null)
        {
            await gate.WaitAsync(ct).ConfigureAwait(false);
            byte[] key = null;
            try
            {
                var init = Request(InitialHeader(order, parameters), new XElement(N + "mutable", new XElement(N + "TransactionPhase", "Initialisation")), new XElement(N + "body"));
                var root = Root(await SendAsync(init, true, ct).ConfigureAwait(false));
                var result = Result(root); if (!result.IsSuccess) return result;
                var id = result.TransactionId;
                if (string.IsNullOrEmpty(id)) throw new InvalidDataException("Missing transaction ID.");
                CheckTransaction(root, id, "Initialisation");
                if (!int.TryParse((string)Static(root)?.Element(N + "NumSegments"), out var segments) || segments < 1 || segments > 100000)
                    throw new InvalidDataException("Invalid segment count.");
                key = TransactionKey(Transfer(root)?.Element(N + "DataEncryptionInfo"));
                var data = new StringBuilder();
                for (var segment = 1; segment <= segments; segment++)
                {
                    if (segment > 1)
                    {
                        root = Root(await SendAsync(TransactionRequest(id, "Transfer", new XElement(N + "body"), segment, segment == segments), true, ct).ConfigureAwait(false));
                        var status = Result(root); if (!status.IsSuccess) return status;
                        CheckTransaction(root, id, "Transfer");
                    }
                    var number = Mutable(root)?.Element(N + "SegmentNumber");
                    if ((int?)number != segment || (bool?)number.Attribute("lastSegment") != (segment == segments))
                        throw new InvalidDataException("Unexpected EBICS segment number or last-segment flag.");
                    var part = Transfer(root)?.Element(N + "OrderData")?.Value ?? throw new InvalidDataException("Missing order data.");
                    if (part.Length == 0 || part.Length > 1024 * 1024 || part.Length % 4 != 0 ||
                        part.Any(c => !(c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '+' || c == '/' || c == '=')))
                        throw new InvalidDataException("Invalid EBICS base64 segment framing.");
                    data.Append(part);
                    if (data.Length > MaximumDownloadBytes * 2L) throw new InvalidDataException("Download exceeds limit.");
                }
                try
                {
                    result.Data = Ebics3Crypto.Decompress(Ebics3Crypto.Aes(Convert.FromBase64String(data.ToString()), key, false), MaximumDownloadBytes);
                    if (beforeReceipt != null) await beforeReceipt(new Ebics3Download(id, result.Data), ct).ConfigureAwait(false);
                }
                catch (Exception failure)
                {
                    try
                    {
                        var negative = TransactionRequest(id, "Receipt", new XElement(N + "body", new XElement(N + "TransferReceipt", new XAttribute("authenticate", true), new XElement(N + "ReceiptCode", "1"))));
                        await SendAsync(negative, true, ct).ConfigureAwait(false);
                    }
                    catch (Exception receiptFailure) { failure.Data["NegativeReceiptFailure"] = receiptFailure.GetType().Name; }
                    throw;
                }
                var receipt = TransactionRequest(id, "Receipt", new XElement(N + "body", new XElement(N + "TransferReceipt", new XAttribute("authenticate", true), new XElement(N + "ReceiptCode", "0"))));
                root = Root(await SendAsync(receipt, true, ct).ConfigureAwait(false));
                CheckTransaction(root, id, "Receipt");
                var acknowledgement = Result(root);
                result.TechnicalReturnCode = acknowledgement.TechnicalReturnCode;
                result.BusinessReturnCode = acknowledgement.BusinessReturnCode;
                result.ReportText = acknowledgement.ReportText;
                result.TransactionPhase = acknowledgement.TransactionPhase;
                result.ReceiptAcknowledged = acknowledgement.IsSuccess;
                return result;
            }
            finally { if (key != null) CryptographicOperations.ZeroMemory(key); gate.Release(); }
        }
        private XElement UserSignature(byte[] digest, IReadOnlyList<UserParams> signers = null)
        {
            signers ??= new[] { config.User };
            var users = new HashSet<string>(StringComparer.Ordinal);
            var signatures = new XElement(S + "UserSignatureData");
            foreach (var user in signers)
            {
                if (user == null || user.PartnerId != config.User.PartnerId || string.IsNullOrWhiteSpace(user.UserId) || !users.Add(user.UserId))
                    throw new ArgumentException("Upload signers must be distinct users of the submitting partner.");
                ValidateUserCertificate(user.SignKeys?.Certificate, Ebics3KeyRole.Signature);
                signatures.Add(new XElement(S + "OrderSignatureData", new XElement(S + "SignatureVersion", user.SignKeys.Version),
                    new XElement(S + "SignatureValue", Convert.ToBase64String(Ebics3Crypto.SignOrderDigest(digest, user.SignKeys))),
                    new XElement(S + "PartnerID", user.PartnerId), new XElement(S + "UserID", user.UserId)));
            }
            return signatures;
        }
        private async Task<Ebics3Response> UploadAsync(string order, XElement parameters, byte[] data, byte[] digestOnly, CancellationToken ct,
            IReadOnlyList<UserParams> signers = null)
        {
            if (SegmentSize < 4 || SegmentSize > 1024 * 1024 || SegmentSize % 4 != 0) throw new ArgumentOutOfRangeException(nameof(SegmentSize));
            await gate.WaitAsync(ct).ConfigureAwait(false);
            var key = RandomNumberGenerator.GetBytes(16);
            try
            {
                var encoded = data == null ? "" : Convert.ToBase64String(Ebics3Crypto.Aes(Ebics3Crypto.Compress(data), key, true));
                var segments = (encoded.Length + SegmentSize - 1) / SegmentSize;
                var stat = InitialHeader(order, parameters);
                stat.Add(new XElement(N + "NumSegments", segments));
                var digest = digestOnly ?? Ebics3Crypto.OrderDigest(data);
                var signature = UserSignature(digest, signers);
                var signatureVersion = signers == null ? config.User.SignKeys.Version : signers[0].SignKeys.Version;
                var transfer = new XElement(N + "DataTransfer",
                    new XElement(N + "DataEncryptionInfo", new XAttribute("authenticate", true),
                        Digest("EncryptionPubKeyDigest", config.Bank.CryptKeys.Certificate, "E002"),
                        new XElement(N + "TransactionKey", Convert.ToBase64String(config.Bank.CryptKeys.PublicKey.Encrypt(key, RSAEncryptionPadding.Pkcs1)))),
                    new XElement(N + "SignatureData", new XAttribute("authenticate", true),
                        Convert.ToBase64String(Ebics3Crypto.Aes(Ebics3Crypto.Compress(Payload(signature)), key, true))));
                // HVE/HVS sign the reviewed original digest, but carry no new order data.
                // 3.0.2 sections 8.3.4.1 and 8.3.5.1 require an empty DataDigest here.
                transfer.Add(new XElement(N + "DataDigest", new XAttribute("SignatureVersion", signatureVersion),
                    order == "HVE" || order == "HVS" ? "" : Convert.ToBase64String(digest)));
                var request = Request(stat, new XElement(N + "mutable", new XElement(N + "TransactionPhase", "Initialisation")), new XElement(N + "body", transfer));
                var root = Root(await SendAsync(request, true, ct).ConfigureAwait(false));
                var result = Result(root); if (!result.IsSuccess) return result;
                var id = result.TransactionId; var orderId = result.OrderId;
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(orderId)) throw new InvalidDataException("Missing upload transaction ID or order ID.");
                CheckTransaction(root, id, "Initialisation");
                if (data == null) return result;
                for (var segment = 1; segment <= segments; segment++)
                {
                    var offset = (segment - 1) * SegmentSize;
                    var part = encoded.Substring(offset, Math.Min(SegmentSize, encoded.Length - offset));
                    request = TransactionRequest(id, "Transfer", new XElement(N + "body", new XElement(N + "DataTransfer", new XElement(N + "OrderData", part))), segment, segment == segments);
                    root = Root(await SendAsync(request, true, ct).ConfigureAwait(false));
                    result = Result(root); if (!result.IsSuccess) return result;
                    CheckTransaction(root, id, "Transfer");
                    if ((int?)Mutable(root)?.Element(N + "SegmentNumber") != segment) throw new InvalidDataException("Upload acknowledgement segment mismatch.");
                    if (result.OrderId != null && result.OrderId != orderId) throw new InvalidDataException("Upload acknowledgement order ID mismatch.");
                }
                result.OrderId = result.OrderId ?? orderId;
                return result;
            }
            finally { CryptographicOperations.ZeroMemory(key); gate.Release(); }
        }
        public void Dispose() { if (ownsHttp) http.Dispose(); gate.Dispose(); }
    }
}
