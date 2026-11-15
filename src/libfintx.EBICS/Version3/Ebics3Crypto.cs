using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using libfintx.EBICSConfig;

namespace libfintx.EBICS.Version3
{
    /// <summary>EBICS H005 cryptographic primitives; no private-key export is required.</summary>
    public static class Ebics3Crypto
    {
        internal const string Ds = "http://www.w3.org/2000/09/xmldsig#";
        internal const string C14n = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
        internal const string Sha256 = "http://www.w3.org/2001/04/xmlenc#sha256";
        internal const string RsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        internal const string Reference = "#xpointer(//*[@authenticate='true'])";

        public static byte[] CertificateDigest(X509Certificate2 certificate)
        {
            if (certificate == null) throw new ArgumentException("H005 requires an X.509 certificate.");
            return SHA256.HashData(certificate.RawData);
        }

        /// <summary>EBICS removes CR, LF and Ctrl-Z before computing the order digest.</summary>
        public static byte[] OrderDigest(byte[] data) => SHA256.HashData(data.Where(b => b != 13 && b != 10 && b != 26).ToArray());

        public static byte[] SignOrderDigest(byte[] digest, SignKeyPair key)
        {
            if (digest == null || digest.Length != 32) throw new ArgumentException("Expected a SHA-256 order digest.");
            if (key?.PrivateKey == null || key.PrivateKey.KeySize < 2048 || key.PrivateKey.KeySize > 4096) throw new CryptographicException("An RSA signing key between 2048 and 4096 bits is required.");
            // A006 hashes the order digest a second time (specification 14.1.4.2.5).
            return key.Version switch
            {
                SignVersion.A006 => key.PrivateKey.SignData(digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
                SignVersion.A005 => key.PrivateKey.SignHash(digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                _ => throw new CryptographicException("Unsupported electronic signature version.")
            };
        }

        internal static byte[] Compress(byte[] bytes)
        {
            using var output = new MemoryStream();
            using (var stream = new ZLibStream(output, CompressionLevel.Optimal, true)) stream.Write(bytes);
            return output.ToArray();
        }
        internal static byte[] Decompress(byte[] bytes, int maximum)
        {
            using var input = new MemoryStream(bytes);
            using var stream = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (output.Length + count > maximum) throw new InvalidDataException("EBICS download exceeds configured limit.");
                output.Write(buffer, 0, count);
            }
            return output.ToArray();
        }
        internal static byte[] Aes(byte[] bytes, byte[] key, bool encrypt)
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = key; aes.IV = new byte[16]; aes.Mode = CipherMode.CBC;
            // EBICS E002 uses X9.23-style output; incoming padding bytes may be arbitrary.
            aes.Padding = encrypt ? PaddingMode.ANSIX923 : PaddingMode.ISO10126;
            using var transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor();
            return transform.TransformFinalBlock(bytes, 0, bytes.Length);
        }

        // EBICS permits leading zero octets to be omitted from X002 signatures and RSA encrypted keys.
        internal static byte[] NormalizeRsaValue(byte[] value, int keyBits)
        {
            int size = (keyBits + 7) / 8;
            if (value.Length == 0 || value.Length > size) throw new CryptographicException("Invalid RSA value length.");
            if (value.Length == size) return value;
            var padded = new byte[size]; value.CopyTo(padded, size - value.Length); return padded;
        }

        private static byte[] Canonicalize(XmlNode node)
        {
            // Preserve in-scope namespaces and xml:* attributes for inclusive canonicalisation.
            var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            var clone = (XmlElement)document.ImportNode(node, true);
            document.AppendChild(clone);
            for (var parent = node.ParentNode as XmlElement; parent != null; parent = parent.ParentNode as XmlElement)
                foreach (XmlAttribute attr in parent.Attributes)
                    if ((attr.Prefix == "xmlns" || attr.Name == "xmlns" || attr.Prefix == "xml") && !clone.HasAttribute(attr.LocalName, attr.NamespaceURI))
                        clone.SetAttribute(attr.Name, attr.Value);
            var transform = new XmlDsigC14NTransform();
            transform.LoadInput(document);
            using var stream = (Stream)transform.GetOutput(typeof(Stream));
            using var bytes = new MemoryStream(); stream.CopyTo(bytes); return bytes.ToArray();
        }
        private static byte[] ReferenceDigest(XmlDocument doc)
        {
            using var buffer = new MemoryStream();
            foreach (XmlNode node in doc.SelectNodes("//*[@authenticate='true']")) buffer.Write(Canonicalize(node));
            return SHA256.HashData(buffer.ToArray());
        }
        public static void Sign(XmlDocument doc, RSA key)
        {
            XNamespace ds = Ds;
            var manager = new XmlNamespaceManager(doc.NameTable); manager.AddNamespace("e", Ebics3Client.Namespace);
            var auth = doc.SelectSingleNode("/*/e:AuthSignature", manager) ?? throw new CryptographicException("Missing AuthSignature.");
            var info = new XElement(ds + "SignedInfo",
                new XElement(ds + "CanonicalizationMethod", new XAttribute("Algorithm", C14n)),
                new XElement(ds + "SignatureMethod", new XAttribute("Algorithm", RsaSha256)),
                new XElement(ds + "Reference", new XAttribute("URI", Reference),
                    new XElement(ds + "Transforms", new XElement(ds + "Transform", new XAttribute("Algorithm", C14n))),
                    new XElement(ds + "DigestMethod", new XAttribute("Algorithm", Sha256)),
                    new XElement(ds + "DigestValue", Convert.ToBase64String(ReferenceDigest(doc)))));
            var fragment = doc.CreateDocumentFragment(); fragment.InnerXml = info.ToString(SaveOptions.DisableFormatting);
            auth.AppendChild(fragment);
            var sig = key.SignData(Canonicalize(auth.FirstChild), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var value = doc.CreateElement("SignatureValue", Ds); value.InnerText = Convert.ToBase64String(sig); auth.AppendChild(value);
        }
        public static void Verify(XmlDocument doc, RSA key)
        {
            var m = new XmlNamespaceManager(doc.NameTable); m.AddNamespace("e", Ebics3Client.Namespace); m.AddNamespace("ds", Ds);
            var auth = doc.SelectNodes("/*/e:AuthSignature", m);
            if (auth.Count != 1 || key == null) throw new CryptographicException("Missing bank authentication signature or trusted key.");
            var info = auth[0].SelectSingleNode("ds:SignedInfo", m);
            var refs = info?.SelectNodes("ds:Reference", m);
            if (info == null || refs.Count != 1 || refs[0].Attributes["URI"]?.Value != Reference ||
                info.SelectSingleNode("ds:CanonicalizationMethod/@Algorithm", m)?.Value != C14n ||
                info.SelectSingleNode("ds:SignatureMethod/@Algorithm", m)?.Value != RsaSha256 ||
                refs[0].SelectSingleNode("ds:DigestMethod/@Algorithm", m)?.Value != Sha256 ||
                refs[0].SelectNodes("ds:Transforms/ds:Transform", m).Count != 1 ||
                refs[0].SelectSingleNode("ds:Transforms/ds:Transform/@Algorithm", m)?.Value != C14n)
                throw new CryptographicException("Unsupported bank signature structure.");
            var expected = Convert.FromBase64String(refs[0].SelectSingleNode("ds:DigestValue", m).InnerText);
            var signature = NormalizeRsaValue(Convert.FromBase64String(auth[0].SelectSingleNode("ds:SignatureValue", m).InnerText), key.KeySize);
            if (!CryptographicOperations.FixedTimeEquals(expected, ReferenceDigest(doc)))
                throw new CryptographicException("Invalid bank authentication reference digest.");
            if (!key.VerifyData(Canonicalize(info), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                throw new CryptographicException("Invalid bank authentication signature value.");
        }
    }
}
