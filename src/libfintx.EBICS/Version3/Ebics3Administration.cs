using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using libfintx.EBICSConfig;
using libfintx.EBICS.Responses;

namespace libfintx.EBICS.Version3
{
    public sealed partial class Ebics3Client
    {
        /// <summary>Discover versions over authenticated TLS; never selects a weaker protocol automatically.</summary>
        public async Task<XElement> HEVAsync(CancellationToken ct = default)
        {
            XNamespace h = "http://www.ebics.org/H000";
            var doc = Parse(new XElement(h + "ebicsHEVRequest", new XElement(h + "HostID", config.User.HostId)).ToString(SaveOptions.DisableFormatting));
            return Root(await SendAsync(doc, false, ct).ConfigureAwait(false));
        }

        /// <summary>Initialise CA-issued certificates together. The bank must enable this procedure.</summary>
        public async Task<Ebics3Response> H3KAsync(CancellationToken ct = default)
        {
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var user = config.User;
                if (user.AuthKeys?.Version != AuthVersion.X002 || user.CryptKeys?.Version != CryptVersion.E002)
                    throw new CryptographicException("H3K requires X002 and E002 certificates.");
                var data = new XElement(N + "H3KRequestOrderData",
                    CertificateInfo(N, "SignatureCertificateInfo", user.SignKeys.Certificate, "SignatureVersion", user.SignKeys.Version.ToString()),
                    CertificateInfo(N, "AuthenticationCertificateInfo", user.AuthKeys.Certificate, "AuthenticationVersion", "X002"),
                    CertificateInfo(N, "EncryptionCertificateInfo", user.CryptKeys.Certificate, "EncryptionVersion", "E002"),
                    new XElement(N + "PartnerID", user.PartnerId), new XElement(N + "UserID", user.UserId));
                var bytes = Payload(data);
                var signature = Payload(UserSignature(Ebics3Crypto.OrderDigest(bytes)));
                var request = Request(InitialHeader("H3K", null, false, false), new XElement(N + "mutable"),
                    new XElement(N + "body", new XElement(N + "DataTransfer",
                        new XElement(N + "SignatureData", new XAttribute("authenticate", true), Convert.ToBase64String(Ebics3Crypto.Compress(signature))),
                        new XElement(N + "OrderData", Convert.ToBase64String(Ebics3Crypto.Compress(bytes))))), "ebicsUnsignedRequest", false);
                return Result(Root(await SendAsync(request, false, ct).ConfigureAwait(false)));
            }
            finally { gate.Release(); }
        }

        private static XElement CertificateInfo(XNamespace ns, string element, X509Certificate2 certificate, string versionElement, string version)
        {
            Ebics3Certificates.Validate(certificate, versionElement == "SignatureVersion" ? Ebics3KeyRole.Signature :
                versionElement == "AuthenticationVersion" ? Ebics3KeyRole.Authentication : Ebics3KeyRole.Encryption);
            return new XElement(ns + element, new XElement(D + "X509Data", new XElement(D + "X509Certificate", Convert.ToBase64String(certificate.RawData))),
                new XElement(ns + versionElement, version));
        }
        private XElement SignatureKey(UserParams user) => CertificateInfo(S, "SignaturePubKeyInfo", user.SignKeys?.Certificate, "SignatureVersion", user.SignKeys.Version.ToString());
        private XElement KeyData(string order, UserParams user)
        {
            if ((order == "HIA" || order == "HCA" || order == "HCS") &&
                (user.AuthKeys?.Version != AuthVersion.X002 || user.CryptKeys?.Version != CryptVersion.E002))
                throw new CryptographicException("H005 key management requires X002 and E002.");
            if (order == "INI" || order == "PUB")
                return new XElement(S + "SignaturePubKeyOrderData", SignatureKey(user), new XElement(S + "PartnerID", config.User.PartnerId), new XElement(S + "UserID", config.User.UserId));
            return new XElement(N + (order + "RequestOrderData"),
                CertificateInfo(N, "AuthenticationPubKeyInfo", user.AuthKeys?.Certificate, "AuthenticationVersion", "X002"),
                CertificateInfo(N, "EncryptionPubKeyInfo", user.CryptKeys?.Certificate, "EncryptionVersion", "E002"),
                order == "HCS" ? SignatureKey(user) : null,
                new XElement(N + "PartnerID", config.User.PartnerId), new XElement(N + "UserID", config.User.UserId));
        }
        public Task<Ebics3Response> INIAsync(CancellationToken ct = default) => InitialiseAsync("INI", ct);
        public Task<Ebics3Response> HIAAsync(CancellationToken ct = default) => InitialiseAsync("HIA", ct);
        private async Task<Ebics3Response> InitialiseAsync(string order, CancellationToken ct)
        {
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var data = Payload(KeyData(order, config.User));
                var request = Request(InitialHeader(order, null, false, false), new XElement(N + "mutable"),
                    new XElement(N + "body", new XElement(N + "DataTransfer", new XElement(N + "OrderData", Convert.ToBase64String(Ebics3Crypto.Compress(data))))),
                    "ebicsUnsecuredRequest", false);
                return Result(Root(await SendAsync(request, false, ct).ConfigureAwait(false)));
            }
            finally { gate.Release(); }
        }

        /// <summary>Fetch bank certificates and verify both against SHA-256 fingerprints received through an independent trusted channel.
        /// Does not install bank keys automatically.</summary>
        public async Task<HpbResponse> HPBAsync(byte[] expectedAuthenticationDigest, byte[] expectedEncryptionDigest, CancellationToken ct = default)
        {
            if (expectedAuthenticationDigest?.Length != 32 || expectedEncryptionDigest?.Length != 32)
                throw new ArgumentException("Both independently verified bank certificate SHA-256 fingerprints are required.");
            await gate.WaitAsync(ct).ConfigureAwait(false);
            byte[] key = null;
            try
            {
                var request = Request(InitialHeader("HPB", null, false), new XElement(N + "mutable"), new XElement(N + "body"), "ebicsNoPubKeyDigestsRequest");
                var document = await SendAsync(request, false, ct).ConfigureAwait(false);
                var root = Root(document); var status = Result(root);
                if (!status.IsSuccess) throw new CryptographicException("HPB did not return verifiable bank certificates: " + status.TechnicalReturnCode + "/" + status.BusinessReturnCode);
                key = TransactionKey(Transfer(root)?.Element(N + "DataEncryptionInfo"));
                var payload = Ebics3Crypto.Decompress(Ebics3Crypto.Aes(Convert.FromBase64String(Transfer(root).Element(N + "OrderData").Value), key, false), MaximumDownloadBytes);
                var payloadDocument = Parse(Encoding.UTF8.GetString(payload));
                Ebics3Schema.Validate(payloadDocument);
                var data = Root(payloadDocument);
                if (data.Name != N + "HPBResponseOrderData" || (string)data.Element(N + "HostID") != config.User.HostId)
                    throw new CryptographicException("Bank key response host mismatch.");
                X509Certificate2 Read(string element, string versionElement, string version, byte[] expected)
                {
                    var info = data.Element(N + element);
                    if ((string)info?.Element(N + versionElement) != version) throw new CryptographicException("Unexpected bank key version.");
                    // A CA chain may be supplied in any order. Select the independently pinned certificate.
                    foreach (var value in info.Element(D + "X509Data").Elements(D + "X509Certificate"))
                    {
                        var cert = new X509Certificate2(Convert.FromBase64String(value.Value));
                        if (!CryptographicOperations.FixedTimeEquals(expected, Ebics3Crypto.CertificateDigest(cert)))
                        { cert.Dispose(); continue; }
                        try { Ebics3Certificates.Validate(cert, version == "X002" ? Ebics3KeyRole.Authentication : Ebics3KeyRole.Encryption); return cert; }
                        catch { cert.Dispose(); throw; }
                    }
                    throw new CryptographicException("Bank certificate fingerprint mismatch.");
                }
                var auth = Read("AuthenticationPubKeyInfo", "AuthenticationVersion", "X002", expectedAuthenticationDigest);
                X509Certificate2 enc = null;
                try
                {
                    enc = Read("EncryptionPubKeyInfo", "EncryptionVersion", "E002", expectedEncryptionDigest);
                    // HPB uses ebicsKeyManagementResponse, which carries no AuthSignature.
                    // Trust is established exclusively by matching BOTH out-of-band certificate fingerprints.
                    return new HpbResponse { TechnicalReturnCode = status.TechnicalReturnCode, BusinessReturnCode = status.BusinessReturnCode,
                        ReportText = status.ReportText, Bank = new BankParams { AuthKeys = new AuthKeyPair { Certificate = auth, Version = AuthVersion.X002 },
                            CryptKeys = new CryptKeyPair { Certificate = enc, Version = CryptVersion.E002 } } };
                }
                catch { auth.Dispose(); enc?.Dispose(); throw; }
            }
            finally { if (key != null) CryptographicOperations.ZeroMemory(key); gate.Release(); }
        }
        /// <summary>Submit new certificates using the current keys. Caller installs new private keys only after bank acknowledgement.</summary>
        public Task<Ebics3Response> ChangeKeysAsync(string order, UserParams newKeys, CancellationToken ct = default)
        {
            if (order != "HCA" && order != "HCS" && order != "PUB") throw new ArgumentException("Use HCA, HCS or PUB.");
            var data = Payload(KeyData(order, newKeys ?? throw new ArgumentNullException(nameof(newKeys))));
            return UploadAsync(order, new XElement(N + "StandardOrderParams"), data, null, ct);
        }
        public Task<Ebics3Response> SPRAsync(CancellationToken ct = default) =>
            UploadAsync("SPR", new XElement(N + "StandardOrderParams"), null, Ebics3Crypto.OrderDigest(new byte[] { 32 }), ct);

        public Task<Ebics3Response> HVDAsync(EdsOrder order, CancellationToken ct = default) =>
            DownloadAdministrativeAsync("HVD", order.ToXml("HVD"), ct);
        public Task<Ebics3Response> HVTAsync(EdsOrder order, bool completeOrderData = false, uint fetchLimit = 100, uint fetchOffset = 0, CancellationToken ct = default)
        {
            var parameters = order.ToXml("HVT");
            parameters.Add(new XElement(N + "OrderFlags", new XAttribute("completeOrderData", completeOrderData), new XAttribute("fetchLimit", fetchLimit), new XAttribute("fetchOffset", fetchOffset)));
            return DownloadAdministrativeAsync("HVT", parameters, ct);
        }
        /// <summary>Authorise the previously reviewed order digest from HVD. This is already a digest: do not hash it again before A006.</summary>
        public Task<Ebics3Response> HVEAsync(EdsOrder order, byte[] reviewedOrderDigest, CancellationToken ct = default) =>
            UploadAsync("HVE", order.ToXml("HVE"), null, reviewedOrderDigest ?? throw new ArgumentNullException(nameof(reviewedOrderDigest)), ct);
        public Task<Ebics3Response> HVSAsync(EdsOrder order, byte[] reviewedOrderDigest, CancellationToken ct = default) =>
            UploadAsync("HVS", order.ToXml("HVS"), null, reviewedOrderDigest ?? throw new ArgumentNullException(nameof(reviewedOrderDigest)), ct);
    }
    public sealed class EdsOrder
    {
        public string PartnerId { get; set; }
        public string OrderId { get; set; }
        public BtfService Service { get; set; }
        internal XElement ToXml(string order)
        {
            if (string.IsNullOrWhiteSpace(PartnerId) || string.IsNullOrWhiteSpace(OrderId) || Service == null) throw new ArgumentException("EDS requires partner, order ID and service.");
            XNamespace n = Ebics3Client.Namespace;
            return new XElement(n + (order + "OrderParams"), new XElement(n + "PartnerID", PartnerId), Service.ToXml(), new XElement(n + "OrderID", OrderId));
        }
    }
}
