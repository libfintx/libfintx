using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using libfintx.EBICS.Version3;
using libfintx.EBICSConfig;

internal static partial class Program
{
    static readonly XNamespace N = Ebics3Client.Namespace;
    static readonly XNamespace D = "http://www.w3.org/2000/09/xmldsig#";
    static int passed;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); passed++; }
    static X509Certificate2 Certificate(int bits = 2048)
    {
        using var rsa = RSA.Create(bits);
        var request = new CertificateRequest("CN=Protocol test only", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.NonRepudiation |
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }
    static Config Config()
    {
        return new Config { Address = "https://bank.invalid/ebics", Version = EbicsVersion.H005,
            User = new UserParams { HostId = "BANK", PartnerId = "PARTNER", UserId = "USER",
                AuthKeys = new AuthKeyPair { Certificate = Certificate(), Version = AuthVersion.X002 },
                CryptKeys = new CryptKeyPair { Certificate = Certificate(), Version = CryptVersion.E002 },
                SignKeys = new SignKeyPair { Certificate = Certificate(), Version = SignVersion.A006 } },
            Bank = new BankParams { AuthKeys = new AuthKeyPair { Certificate = Certificate(), Version = AuthVersion.X002 },
                CryptKeys = new CryptKeyPair { Certificate = Certificate(), Version = CryptVersion.E002 } } };
    }
    static XmlDocument Document(XElement root) { var doc = new XmlDocument { PreserveWhitespace = true }; doc.LoadXml(root.ToString(SaveOptions.DisableFormatting)); return doc; }
    static byte[] Compress(byte[] b) { using var m = new MemoryStream(); using(var z = new ZLibStream(m, CompressionLevel.Optimal, true)) z.Write(b); return m.ToArray(); }
    static byte[] Decompress(byte[] b) { using var z = new ZLibStream(new MemoryStream(b), CompressionMode.Decompress); using var m = new MemoryStream(); z.CopyTo(m); return m.ToArray(); }
    static byte[] Aes(byte[] b, byte[] key, bool encrypt)
    { using var a = System.Security.Cryptography.Aes.Create(); a.Key = key; a.IV = new byte[16]; a.Padding = PaddingMode.ANSIX923; using var t = encrypt ? a.CreateEncryptor() : a.CreateDecryptor(); return t.TransformFinalBlock(b,0,b.Length); }
    static XElement Response(string phase, int? segment = null, int? count = null, XElement transfer = null, string tech = "000000", string business = "000000") =>
        new XElement(N + "ebicsResponse", new XAttribute("Version", "H005"), new XAttribute("Revision", "1"),
            new XElement(N + "header", new XAttribute("authenticate", true),
                new XElement(N + "static", new XElement(N + "TransactionID", "0123456789ABCDEF0123456789ABCDEF"), count.HasValue ? new XElement(N + "NumSegments", count) : null),
                new XElement(N + "mutable", new XElement(N + "TransactionPhase", phase),
                    segment.HasValue ? new XElement(N + "SegmentNumber", new XAttribute("lastSegment", segment == count), segment) : null,
                    new XElement(N + "OrderID", "A001"),
                    new XElement(N + "ReturnCode", tech), new XElement(N + "ReportText", "test"))),
            new XElement(N + "AuthSignature"), new XElement(N + "body", transfer, new XElement(N + "ReturnCode", new XAttribute("authenticate", true), business)));
    sealed class Bank : HttpMessageHandler
    {
        public Func<XElement, XElement> Handle;
        public Config Config;
        public bool Tamper;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var text = await request.Content.ReadAsStringAsync(ct);
            var doc = new XmlDocument { PreserveWhitespace = true }; doc.LoadXml(text);
            if (doc.GetElementsByTagName("AuthSignature",Ebics3Client.Namespace).Count > 0) Ebics3Crypto.Verify(doc, Config.User.AuthKeys.PublicKey);
            var response = Document(Handle(XElement.Parse(text)));
            if (response.GetElementsByTagName("AuthSignature",Ebics3Client.Namespace).Count > 0) Ebics3Crypto.Sign(response, Config.Bank.AuthKeys.PrivateKey);
            if (Tamper) response.DocumentElement.SetAttribute("tampered", "true");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response.OuterXml) };
        }
    }
    static BtfService Service() => new BtfService { ServiceName = "EOP", Scope = "DE", MessageName = "camt.053", MessageVersion = "08", ContainerType = "ZIP" };
    static async Task Main()
    {
        foreach (var bits in new[] { 2048, 3072, 4096 })
        {
            using var cert = Certificate(bits); using var rsa = cert.GetRSAPrivateKey();
            var key = new SignKeyPair { Certificate = cert, Version = SignVersion.A006 };
            var digest = Ebics3Crypto.OrderDigest(Encoding.UTF8.GetBytes("a\r\nb\u001ac\td"));
            Check(digest.SequenceEqual(SHA256.HashData(Encoding.UTF8.GetBytes("abc\td"))), "Order normalisation");
            var signature = Ebics3Crypto.SignOrderDigest(digest, key);
            Check(signature.Length == bits / 8 && rsa.VerifyData(digest, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss), "A006 double hash and key length");
            Check(!rsa.VerifyHash(digest, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss), "Must not single-hash A006");
        }
        using (var certificate = new X509Certificate2(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures/x002-certificate.cer"))))
        using (var rsa = certificate.GetRSAPublicKey())
        {
            var fixture = new XmlDocument { PreserveWhitespace=true };
            fixture.Load(Path.Combine(AppContext.BaseDirectory,"Fixtures/x002-response.xml"));
            Ebics3Crypto.Verify(fixture,rsa); passed++;
            fixture.Load(Path.Combine(AppContext.BaseDirectory,"Fixtures/x002-inherited-short-response.xml"));
            Ebics3Crypto.Verify(fixture,rsa); passed++;
        }
        var cfg = Config();
        var document = Document(Response("Initialisation",1,1));
        Ebics3Crypto.Sign(document, cfg.Bank.AuthKeys.PrivateKey); Ebics3Crypto.Verify(document,cfg.Bank.AuthKeys.PublicKey); passed++;
        var node = document.GetElementsByTagName("ReportText", Ebics3Client.Namespace)[0]; node.InnerText = "tampered";
        try { Ebics3Crypto.Verify(document, cfg.Bank.AuthKeys.PublicKey); throw new Exception("Accepted tamper"); } catch (CryptographicException) { passed++; }
        var original = RandomNumberGenerator.GetBytes(15000);
        var transactionKey = RandomNumberGenerator.GetBytes(16);
        var encrypted = Convert.ToBase64String(Aes(Compress(original), transactionKey, true));
        const int size = 1024;
        int segments = (encrypted.Length + size - 1) / size;
        int requested = 0; bool receipt = false;
        var bank = new Bank { Config = cfg };
        bank.Handle = req =>
        {
            var phase = req.Element(N+"header").Element(N+"mutable").Element(N+"TransactionPhase").Value;
            if (phase == "Receipt") { receipt = true; return Response(phase, tech: "011000"); }
            int segment = phase == "Initialisation" ? 1 : (int)req.Descendants(N+"SegmentNumber").Single();
            Check(segment == ++requested, "Segments must be sequential");
            var part = encrypted.Substring((segment-1)*size, Math.Min(size,encrypted.Length-(segment-1)*size));
            var info = segment == 1 ? new XElement(N+"DataEncryptionInfo", new XAttribute("authenticate",true),
                new XElement(N+"EncryptionPubKeyDigest",new XAttribute("Version","E002"),new XAttribute("Algorithm","http://www.w3.org/2001/04/xmlenc#sha256"),Convert.ToBase64String(Ebics3Crypto.CertificateDigest(cfg.User.CryptKeys.Certificate))),
                new XElement(N+"TransactionKey",Convert.ToBase64String(cfg.User.CryptKeys.PublicKey.Encrypt(transactionKey,RSAEncryptionPadding.Pkcs1)))) : null;
            return Response(phase,segment,segments,new XElement(N+"DataTransfer",info,new XElement(N+"OrderData",part)));
        };
        using(var http = new HttpClient(bank)) using(var client = new Ebics3Client(cfg,http))
        {
            var result = await client.BTDAsync(new BtdRequest { Service=Service() });
            Check(result.IsSuccess && result.ReceiptAcknowledged && receipt && result.Data.SequenceEqual(original),"Segmented download round trip");
        }
        byte[] uploadKey = null; var upload = new StringBuilder(); int uploadSegments = 0;
        bank = new Bank { Config = cfg, Handle = req =>
        {
            var phase = req.Descendants(N+"TransactionPhase").Single().Value;
            if (phase == "Initialisation")
            {
                uploadSegments = (int)req.Descendants(N+"NumSegments").Single();
                uploadKey = cfg.Bank.CryptKeys.PrivateKey.Decrypt(Convert.FromBase64String(req.Descendants(N+"TransactionKey").Single().Value),RSAEncryptionPadding.Pkcs1);
                var signatureXml = XElement.Parse(Encoding.UTF8.GetString(Decompress(Aes(Convert.FromBase64String(req.Descendants(N+"SignatureData").Single().Value),uploadKey,false))));
                XNamespace s = Ebics3Client.SignatureNamespace;
                Check(cfg.User.SignKeys.PublicKey.VerifyData(Ebics3Crypto.OrderDigest(original),Convert.FromBase64String(signatureXml.Descendants(s+"SignatureValue").Single().Value),HashAlgorithmName.SHA256,RSASignaturePadding.Pss),"Upload signature verifies");
                return Response(phase);
            }
            int segment=(int)req.Descendants(N+"SegmentNumber").Single(); upload.Append(req.Descendants(N+"OrderData").Single().Value);
            return Response(phase,segment,uploadSegments);
        }};
        using(var http = new HttpClient(bank)) using(var client = new Ebics3Client(cfg,http) { SegmentSize = 1024 })
        {
            var result = await client.BTUAsync(new BtuRequest { Service=Service(),Data=original,RequestDistributedSignature=true });
            Check(result.IsSuccess && Decompress(Aes(Convert.FromBase64String(upload.ToString()),uploadKey,false)).SequenceEqual(original),"Segmented upload round trip");
        }
        bank = new Bank { Config=cfg, Handle=req =>
        {
            var order=req.Descendants(N+"AdminOrderType").Single().Value;
            var bytes=Decompress(Convert.FromBase64String(req.Descendants(N+"OrderData").Single().Value));
            var data=XElement.Parse(Encoding.UTF8.GetString(bytes));
            Check(data.Descendants(D+"X509Certificate").Count() == (order=="INI" ? 1 : order=="HIA" ? 2 : 3),"Certificates in key-management payload");
            Check(!data.Descendants().Any(e=>e.Name.LocalName=="RSAKeyValue"),"H005 must not send raw RSA keys");
            return new XElement(N+"ebicsKeyManagementResponse",new XAttribute("Version","H005"),new XAttribute("Revision","1"),
                new XElement(N+"header",new XAttribute("authenticate",true),new XElement(N+"static"),
                    new XElement(N+"mutable",new XElement(N+"ReturnCode","000000"),new XElement(N+"ReportText","OK"))),
                new XElement(N+"body",new XElement(N+"ReturnCode",new XAttribute("authenticate",true),"000000")));
        }};
        using(var http=new HttpClient(bank)) using(var client=new Ebics3Client(cfg,http))
        {
            Check((await client.INIAsync()).IsSuccess,"INI");
            Check((await client.HIAAsync()).IsSuccess,"HIA");
            Check((await client.H3KAsync()).IsSuccess,"H3K");
        }
        foreach(var code in new[]{"091005","091301","061101"})
        {
            bank=new Bank {Config=cfg,Handle=req=>Response("Initialisation",tech:code)};
            using var http=new HttpClient(bank); using var client=new Ebics3Client(cfg,http);
            Check(!(await client.BTDAsync(new BtdRequest {Service=Service()})).IsSuccess,"Error/recovery is never success");
        }
        bank=new Bank {Config=cfg,Handle=req=>Response("Initialisation",tech:"091005"),Tamper=true};
        using(var http=new HttpClient(bank)) using(var client=new Ebics3Client(cfg,http))
        {
            try {await client.BTDAsync(new BtdRequest {Service=Service(),StartDate=DateTime.Now});throw new Exception("Accepted incomplete dates");}
            catch(ArgumentException){passed++;}
        }
        bank=new Bank {Config=cfg,Handle=req=>Response("Initialisation",tech:"091005")};
        using(var http=new HttpClient(bank)) using(var client=new Ebics3Client(cfg,http))
        {
            foreach(var order in new[]{"HAA","HAC","HPD","HKD","HTD","HVU","HVZ"})
                Check(!(await client.DownloadAdministrativeAsync(order)).IsSuccess,"Administrative request schema: "+order);
            try { await client.DownloadAdministrativeAsync("PTK"); throw new Exception("Accepted obsolete PTK"); }
            catch (ArgumentException) { passed++; }
            var eds=new EdsOrder {PartnerId="PARTNER",OrderId="A001",Service=Service()};
            Check(!(await client.HVDAsync(eds)).IsSuccess,"HVD schema");
            Check(!(await client.HVTAsync(eds)).IsSuccess,"HVT schema");
            Check(!(await client.HVEAsync(eds,SHA256.HashData(original))).IsSuccess,"HVE schema");
            Check(!(await client.HVSAsync(eds,SHA256.HashData(original))).IsSuccess,"HVS schema");
            Check(!(await client.SPRAsync()).IsSuccess,"SPR schema");
            foreach(var order in new[]{"HCA","HCS","PUB"})
                Check(!(await client.ChangeKeysAsync(order,cfg.User)).IsSuccess,"Key change schema: "+order);
        }
        bank=new Bank {Config=cfg,Handle=req=>
        {
            var data=new XElement(N+"HPBResponseOrderData",
                new XElement(N+"AuthenticationPubKeyInfo",new XElement(D+"X509Data",new XElement(D+"X509Certificate",Convert.ToBase64String(cfg.User.AuthKeys.Certificate.RawData)),new XElement(D+"X509Certificate",Convert.ToBase64String(cfg.Bank.AuthKeys.Certificate.RawData))),new XElement(N+"AuthenticationVersion","X002")),
                new XElement(N+"EncryptionPubKeyInfo",new XElement(D+"X509Data",new XElement(D+"X509Certificate",Convert.ToBase64String(cfg.Bank.CryptKeys.Certificate.RawData))),new XElement(N+"EncryptionVersion","E002")),
                new XElement(N+"HostID","BANK"));
            return new XElement(N+"ebicsKeyManagementResponse",new XAttribute("Version","H005"),new XAttribute("Revision","1"),
                new XElement(N+"header",new XAttribute("authenticate",true),new XElement(N+"static"),new XElement(N+"mutable",new XElement(N+"ReturnCode","000000"),new XElement(N+"ReportText","OK"))),
                new XElement(N+"body",new XElement(N+"DataTransfer",
                    new XElement(N+"DataEncryptionInfo",new XAttribute("authenticate",true),
                        new XElement(N+"EncryptionPubKeyDigest",new XAttribute("Version","E002"),new XAttribute("Algorithm","http://www.w3.org/2001/04/xmlenc#sha256"),Convert.ToBase64String(Ebics3Crypto.CertificateDigest(cfg.User.CryptKeys.Certificate))),
                        new XElement(N+"TransactionKey",Convert.ToBase64String(cfg.User.CryptKeys.PublicKey.Encrypt(transactionKey,RSAEncryptionPadding.Pkcs1)))),
                    new XElement(N+"OrderData",Convert.ToBase64String(Aes(Compress(Encoding.UTF8.GetBytes(data.ToString(SaveOptions.DisableFormatting))),transactionKey,true)))),
                    new XElement(N+"ReturnCode",new XAttribute("authenticate",true),"000000")));
        }};
        using(var http=new HttpClient(bank)) using(var client=new Ebics3Client(cfg,http))
        {
            var auth=Ebics3Crypto.CertificateDigest(cfg.Bank.AuthKeys.Certificate);
            var enc=Ebics3Crypto.CertificateDigest(cfg.Bank.CryptKeys.Certificate);
            var result=await client.HPBAsync(auth,enc);
            Check(result.Bank.AuthKeys.Certificate.RawData.SequenceEqual(cfg.Bank.AuthKeys.Certificate.RawData),"HPB pinned bank certificate");
            auth[0]^=1;
            try {await client.HPBAsync(auth,enc);throw new Exception("Accepted wrong bank pin");}catch(CryptographicException){passed++;}
            auth[0]^=1; enc[0]^=1;
            try {await client.HPBAsync(auth,enc);throw new Exception("Accepted wrong encryption pin");}catch(CryptographicException){passed++;}
        }
        // A well-formed signed response for the wrong transaction must not advance the state machine.
        bank=new Bank {Config=cfg,Handle=req=>
        {
            var r=Response("Transfer",1,1); return r;
        }};
        using(var http=new HttpClient(bank)) using(var client=new Ebics3Client(cfg,http))
        {
            try {await client.BTDAsync(new BtdRequest {Service=Service()});throw new Exception("Accepted wrong phase");}
            catch(InvalidDataException){passed++;}
        }
        bank=new Bank {Config=cfg,Handle=req=>
        {
            var r=Response("Initialisation",tech:"091005");r.Element(N+"body").Element(N+"ReturnCode").Remove();return r;
        }};
        using(var http=new HttpClient(bank)) using(var client=new Ebics3Client(cfg,http))
        {
            try {await client.BTDAsync(new BtdRequest {Service=Service()});throw new Exception("Accepted missing return code");}
            catch(System.Xml.Schema.XmlSchemaValidationException){passed++;}
        }
        byte[] stored=null; cfg.Bank.AuthKeys.Save(bytes=>stored=bytes);
        using(var reloaded=new AuthKeyPair())
        {
            reloaded.Load(stored);
            Check(Ebics3Crypto.CertificateDigest(reloaded.Certificate).SequenceEqual(Ebics3Crypto.CertificateDigest(cfg.Bank.AuthKeys.Certificate)),"Certificate survives persistence");
            Check(reloaded.PrivateKey==null,"No private key persisted");
        }
        // Legacy public-key PEM remains readable.
        using(var legacy=new AuthKeyPair {PublicKey=cfg.Bank.AuthKeys.PublicKey})
        { legacy.Save(bytes=>stored=bytes); using var loaded=new AuthKeyPair(); loaded.Load(stored); Check(loaded.Modulus.SequenceEqual(legacy.Modulus),"H004 PEM persistence"); }
        await RevisionTests();
        Console.WriteLine($"PASS: {passed} assertions (crypto, signature tamper, segmented upload/download, schemas, receipt, EBICS 3.0.2 revisions)");
    }
}
