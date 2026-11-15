using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using libfintx.EBICS.Version3;
using libfintx.EBICSConfig;

internal static partial class Program
{
    private static XElement EncryptedDownload(Config cfg, byte[] key, byte[] compressed, byte[] encryptedKey = null)
    {
        var recipient = cfg.User.CryptKeys;
        return Response("Initialisation", 1, 1, new XElement(N + "DataTransfer",
            new XElement(N + "DataEncryptionInfo", new XAttribute("authenticate", true),
                new XElement(N + "EncryptionPubKeyDigest", new XAttribute("Version", "E002"),
                    new XAttribute("Algorithm", "http://www.w3.org/2001/04/xmlenc#sha256"),
                    Convert.ToBase64String(recipient.Certificate == null ? recipient.Digest : Ebics3Crypto.CertificateDigest(recipient.Certificate))),
                new XElement(N + "TransactionKey", Convert.ToBase64String(encryptedKey ?? recipient.PublicKey.Encrypt(key, RSAEncryptionPadding.Pkcs1)))),
            new XElement(N + "OrderData", Convert.ToBase64String(Aes(compressed, key, true)))));
    }

    static async Task RevisionTests()
    {
        var cfg = Config();
        var digest = Ebics3Crypto.OrderDigest(Encoding.UTF8.GetBytes("Reviewed order"));
        cfg.User.SystemId = "SYSTEM";
        var bank = new Bank { Config = cfg, Handle = request =>
        {
            Check(request.Descendants(N + "NumSegments").Single().Value == "0", "Signature-only uploads declare zero segments");
            Check(request.Descendants(N + "DataDigest").Single().Value == "", "HVE/HVS DataDigest is empty");
            Check(request.Descendants(N + "SystemID").Single().Value == "SYSTEM", "Technical subscriber is transmitted");
            Check(request.Descendants(N + "SecurityMedium").Single().Value == "0200", "Security medium is transmitted");
            var key = cfg.Bank.CryptKeys.PrivateKey.Decrypt(Convert.FromBase64String(request.Descendants(N + "TransactionKey").Single().Value), RSAEncryptionPadding.Pkcs1);
            var signature = XElement.Parse(Encoding.UTF8.GetString(Decompress(Aes(Convert.FromBase64String(request.Descendants(N + "SignatureData").Single().Value), key, false))));
            XNamespace s = Ebics3Client.SignatureNamespace;
            Check(cfg.User.SignKeys.PublicKey.VerifyData(digest, Convert.FromBase64String(signature.Descendants(s + "SignatureValue").Single().Value), HashAlgorithmName.SHA256, RSASignaturePadding.Pss), "HVE/HVS signs the reviewed original digest");
            return Response("Initialisation");
        }};
        using (var http = new HttpClient(bank)) using (var client = new Ebics3Client(cfg, http) { SecurityMedium = "0200" })
        {
            var order = new EdsOrder { PartnerId = "PARTNER", OrderId = "A001", Service = Service() };
            Check((await client.HVEAsync(order, digest)).IsSuccess, "HVE completes in initialisation");
            Check((await client.HVSAsync(order, digest)).IsSuccess, "HVS completes in initialisation");
        }
        cfg.User.SystemId = null;
        var data = Encoding.UTF8.GetBytes("Store these bytes");
        var transactionKey = RandomNumberGenerator.GetBytes(16);
        var otherSigner = new UserParams { PartnerId = cfg.User.PartnerId, UserId = "SECOND",
            SignKeys = new SignKeyPair { Certificate = Certificate(), Version = SignVersion.A005 } };
        bank = new Bank { Config = cfg, Handle = request => {
            var key = cfg.Bank.CryptKeys.PrivateKey.Decrypt(Convert.FromBase64String(request.Descendants(N + "TransactionKey").Single().Value), RSAEncryptionPadding.Pkcs1);
            var signatures = XElement.Parse(Encoding.UTF8.GetString(Decompress(Aes(Convert.FromBase64String(request.Descendants(N + "SignatureData").Single().Value), key, false))));
            XNamespace s = Ebics3Client.SignatureNamespace;
            var records = signatures.Elements(s + "OrderSignatureData").ToArray();
            Check(records.Length == 2, "Two ES signatures are transmitted together");
            var hash = Ebics3Crypto.OrderDigest(data);
            Check(cfg.User.SignKeys.PublicKey.VerifyData(hash, Convert.FromBase64String(records[0].Element(s + "SignatureValue").Value), HashAlgorithmName.SHA256, RSASignaturePadding.Pss), "First A006 signature verifies");
            Check(otherSigner.SignKeys.PublicKey.VerifyHash(hash, Convert.FromBase64String(records[1].Element(s + "SignatureValue").Value), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1), "Second A005 signature verifies");
            return Response("Initialisation", tech: "091005");
        }};
        using (var http = new HttpClient(bank)) using (var client = new Ebics3Client(cfg, http))
        {
            var request = new BtuRequest { Service = Service(), Data = data };
            request.Signers.Add(cfg.User); request.Signers.Add(otherSigner);
            await client.BTUAsync(request);
            request.Signers.Add(cfg.User);
            try { await client.BTUAsync(request); throw new Exception("Accepted duplicate signer"); }
            catch (ArgumentException) { passed++; }
        }
        foreach (var code in new[] { "000000", "011001", "011000" })
        {
            bank = new Bank { Config = cfg, Handle = request => request.Descendants(N + "TransactionPhase").Single().Value == "Receipt"
                ? Response("Receipt", tech: code) : EncryptedDownload(cfg, transactionKey, Compress(data)) };
            using var http = new HttpClient(bank); using var client = new Ebics3Client(cfg, http);
            var result = await client.BTDAsync(new BtdRequest { Service = Service() });
            Check(result.IsSuccess == (code == "011000") && result.ReceiptAcknowledged == (code == "011000"), "Only 011000 acknowledges a positive receipt");
        }
        bool stored = false;
        bank = new Bank { Config = cfg, Handle = request => {
            if (request.Descendants(N + "TransactionPhase").Single().Value != "Receipt") return EncryptedDownload(cfg, transactionKey, Compress(data));
            Check(stored, "Positive receipt is sent only after persistence callback");
            return Response("Receipt", tech: "011000");
        }};
        using (var http = new HttpClient(bank)) using (var client = new Ebics3Client(cfg, http))
            Check((await client.BTDAsync(new BtdRequest { Service = Service(), BeforeReceipt = (download, ct) => {
                Check(download.TransactionId.Length == 32 && download.Data.Span.SequenceEqual(data), "Persistence receives original bytes and transaction ID");
                stored = true; return Task.CompletedTask;
            }})).IsSuccess, "Persisted download completes");
        bool rejected = false;
        bank = new Bank { Config = cfg, Handle = request => {
            if (request.Descendants(N + "TransactionPhase").Single().Value != "Receipt") return EncryptedDownload(cfg, transactionKey, Compress(data));
            rejected = request.Descendants(N + "ReceiptCode").Single().Value == "1";
            return Response("Receipt", tech: "011001");
        }};
        using (var http = new HttpClient(bank)) using (var client = new Ebics3Client(cfg, http))
        {
            try { await client.BTDAsync(new BtdRequest { Service = Service(), BeforeReceipt = (_, _) => throw new IOException("Simulated persistence failure") }); throw new Exception("Ignored persistence failure"); }
            catch (IOException) { Check(rejected, "Persistence failure sends a negative receipt"); }
        }
        bool negative = false;
        bank = new Bank { Config = cfg, Handle = request => {
            if (request.Descendants(N + "TransactionPhase").Single().Value != "Receipt")
                return EncryptedDownload(cfg, transactionKey, new byte[] { 1, 2, 3, 4 });
            negative = request.Descendants(N + "ReceiptCode").Single().Value == "1";
            return Response("Receipt", tech: "011001");
        }};
        using (var http = new HttpClient(bank)) using (var client = new Ebics3Client(cfg, http))
        {
            try { await client.BTDAsync(new BtdRequest { Service = Service() }); throw new Exception("Accepted corrupt zlib data"); }
            catch (InvalidDataException) { Check(negative, "Corrupt data produces a negative receipt"); }
        }
        // EBICS encodes RSA integers without requiring leading zero octets.
        byte[] shortKey;
        do { shortKey = cfg.User.CryptKeys.PublicKey.Encrypt(transactionKey, RSAEncryptionPadding.Pkcs1); } while (shortKey[0] != 0);
        shortKey = shortKey.SkipWhile(b => b == 0).ToArray();
        bank = new Bank { Config = cfg, Handle = request => request.Descendants(N + "TransactionPhase").Single().Value == "Receipt"
            ? Response("Receipt", tech: "011000") : EncryptedDownload(cfg, transactionKey, Compress(data), shortKey) };
        using (var http = new HttpClient(bank)) using (var client = new Ebics3Client(cfg, http))
            Check((await client.BTDAsync(new BtdRequest { Service = Service() })).Data.SequenceEqual(data), "Unpadded RSA encrypted transaction key");
        cfg.User.AuthKeys.Certificate = null; cfg.User.CryptKeys.Certificate = null; cfg.User.SignKeys.Certificate = null;
        using (var http = new HttpClient(bank)) using (var client = new Ebics3Client(cfg, http))
        {
            try { await client.BTDAsync(new BtdRequest { Service = Service() }); throw new Exception("Implicit raw-key migration"); }
            catch (CryptographicException) { passed++; }
            client.AllowMigratedUserKeys = true;
            Check((await client.BTDAsync(new BtdRequest { Service = Service() })).IsSuccess, "Explicit migrated user keys use legacy recipient digest");
            try { await client.HIAAsync(); throw new Exception("Allowed raw-key enrolment"); }
            catch (CryptographicException) { passed++; }
        }
        foreach (var role in Enum.GetValues<Ebics3KeyRole>())
        {
            using var rsa = RSA.Create(2048);
            using var certificate = Ebics3Certificates.CreateSelfSigned("CN=Role test", rsa, role, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5));
            Ebics3Certificates.Validate(certificate, role); passed++;
            Check(certificate.Extensions.Cast<X509Extension>().Any(e => e.Oid.Value == "2.5.29.35"), "Authority key identifier included");
            try { Ebics3Certificates.Validate(certificate, role == Ebics3KeyRole.Signature ? Ebics3KeyRole.Authentication : Ebics3KeyRole.Signature); throw new Exception("Accepted wrong role"); }
            catch (CryptographicException) { passed++; }
        }
        using (var rsa = RSA.Create(2048))
        {
            var request = new CertificateRequest("CN=Expired", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            using var expired = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1));
            try { Ebics3Certificates.Validate(expired, Ebics3KeyRole.Authentication); throw new Exception("Accepted expired certificate"); }
            catch (CryptographicException) { passed++; }
        }
        using (var handler = Ebics3Tls.CreateHandler())
        {
            Check(!handler.AllowAutoRedirect && handler.SslOptions.CertificateRevocationCheckMode == X509RevocationMode.Online, "TLS validates revocation and forbids redirects");
            Check(handler.SslOptions.RemoteCertificateValidationCallback(null, null, null, SslPolicyErrors.RemoteCertificateNameMismatch) == false, "TLS never ignores hostname failures");
            if (OperatingSystem.IsLinux()) Check(handler.SslOptions.CipherSuitesPolicy.AllowedCipherSuites.All(s => !s.ToString().StartsWith("TLS_RSA_")), "Linux defaults require forward secrecy");
        }
        using (var root = Certificate()) using (var handler = Ebics3Tls.CreateHandler(new X509Certificate2Collection(root)))
            Check(handler.SslOptions.CertificateChainPolicy.TrustMode == X509ChainTrustMode.CustomRootTrust && handler.SslOptions.CertificateChainPolicy.CustomTrustStore.Count == 1, "Bank trust anchors are scoped to the handler");
        cfg = Config();
        using (var auth = new NonExportableRsa(cfg.User.AuthKeys.PrivateKey))
        using (var sign = new NonExportableRsa(cfg.User.SignKeys.PrivateKey))
        {
            cfg.User.AuthKeys.PrivateKey = auth; cfg.User.SignKeys.PrivateKey = sign;
            bank = new Bank { Config = cfg, Handle = request => Response("Initialisation", tech: "091005") };
            using var http = new HttpClient(bank); using var client = new Ebics3Client(cfg, http);
            Check(!(await client.BTUAsync(new BtuRequest { Service = Service(), Data = data })).IsSuccess && auth.Operations > 0 && sign.Operations > 0,
                "Authentication and order signing work with a provider that forbids key export");
        }
        HacTests();
    }

    static void HacTests()
    {
        XNamespace n = "urn:iso:std:iso:20022:tech:xsd:pain.002.001.03";
        XElement Step(string action, string reason = null) => new XElement(n + "OrgnlPmtInfAndSts", new XElement(n + "OrgnlPmtInfId", action),
            new XElement(n + "StsRsnInf", reason == null ? null : new XElement(n + "Rsn", new XElement(n + "Cd", reason)),
                new XElement(n + "Orgtr", new XElement(n + "Id", new XElement(n + "OrgId", new XElement(n + "Othr",
                    new XElement(n + "Id", "A001"), new XElement(n + "SchmeNm", new XElement(n + "Prtry", "OrderID"))))))));
        byte[] DocumentBytes(params XElement[] steps) => Encoding.UTF8.GetBytes(new XElement(n + "Document", new XElement(n + "CstmrPmtStsRpt", steps)).ToString());
        var steps = HacAcknowledgement.Parse(DocumentBytes(Step("VEU_CANCEL_ORDER", "DS02"), Step("ORDER_HAC_FINAL_POS"), Step("ORDER_HAC_FINAL_NEG"), Step("ORDER_HAC_FINAL")));
        Check(steps.Count == 4 && steps[0].ReasonCode == "DS02" && steps[1].Completion == HacCompletion.Positive && steps[2].Completion == HacCompletion.Negative && steps[3].Completion == HacCompletion.LegacyNeutral, "HAC 3.0.2 distinguishes completion from payment settlement");
        Check(steps.All(s => s.Identifiers["OrderID"].Single() == "A001"), "HAC retains per-step identifiers");
        try { HacAcknowledgement.Parse(DocumentBytes(Step("ORDER_HAC_FINAL_POS", "DS05"))); throw new Exception("Accepted reason on final HAC"); }
        catch (InvalidDataException) { passed++; }
        try { HacAcknowledgement.Parse(DocumentBytes(Step("VEU_VERIFICATION_END", "DS0B"))); throw new Exception("Accepted failed final signature verification"); }
        catch (InvalidDataException) { passed++; }
        try { HacAcknowledgement.Parse(Encoding.UTF8.GetBytes("<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///not-readable'>]><x>&e;</x>")); throw new Exception("Accepted HAC DTD"); }
        catch (XmlException) { passed++; }
    }

    // Contract test for a hardware/remote RSA adapter; no ExportParameters operation is supported.
    sealed class NonExportableRsa : RSA
    {
        private readonly RSA provider;
        public int Operations { get; private set; }
        public NonExportableRsa(RSA provider) { this.provider = provider; }
        public override int KeySize { get => provider.KeySize; set => throw new NotSupportedException(); }
        public override RSAParameters ExportParameters(bool includePrivateParameters) => throw new CryptographicException("Key export forbidden");
        public override void ImportParameters(RSAParameters parameters) => throw new NotSupportedException();
        public override byte[] SignHash(byte[] hash, HashAlgorithmName algorithm, RSASignaturePadding padding) { Operations++; return provider.SignHash(hash, algorithm, padding); }
        public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName algorithm, RSASignaturePadding padding) => provider.VerifyHash(hash, signature, algorithm, padding);
        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) => provider.Encrypt(data, padding);
        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) { Operations++; return provider.Decrypt(data, padding); }
    }
}
