using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using libfintx.Xml;
using System.Xml.Linq;
using System.Xml;
using libfintx.EBICSConfig;
using Microsoft.Extensions.Logging;
using libfintx.EBICS.Exceptions;
using System.Collections;


namespace libfintx.EBICS.Commands
{
    public class XmlCommand
    {
        public static NamespaceConfig Namespaces { get; set; }

        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<XmlCommand>();

        internal Config Config { get; set; }

        protected static string s_signatureAlg => "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        public  static string s_digestAlg => "http://www.w3.org/2001/04/xmlenc#sha256";

        public XmlDocument AuthenticateXml(XmlDocument doc, string referenceUri,
            IDictionary<string, string> cnm)
        {
            using (new XmlMethodLogger(s_logger))
            {
                doc.PreserveWhitespace = true;
                var sigDoc = new CustomSignedXml(doc)
                {
                    SignatureKey = Config.User.AuthKeys.PrivateKey,
                    SignaturePadding = RSASignaturePadding.Pkcs1,
                    CanonicalizationAlgorithm = SignedXml.XmlDsigC14NTransformUrl,
                    SignatureAlgorithm = s_signatureAlg,
                    DigestAlgorithm = s_digestAlg,
                    ReferenceUri = referenceUri ?? CustomSignedXml.DefaultReferenceUri
                };

                var nm = new XmlNamespaceManager(doc.NameTable);
                nm.AddNamespace(Namespaces.EbicsPrefix, Namespaces.Ebics);
                if (cnm != null && cnm.Count > 0)
                {
                    foreach (var kv in cnm)
                    {
                        nm.AddNamespace(kv.Key, kv.Value);
                    }
                }
                sigDoc.NamespaceManager = nm;
                sigDoc.ComputeSignature();

                var xmlDigitalSignature = sigDoc.GetXml();
                var headerNode = doc.SelectSingleNode($"//{Namespaces.EbicsPrefix}:{XmlNames.AuthSignature}", nm);
                foreach (XmlNode child in xmlDigitalSignature.ChildNodes)
                {
                    headerNode.AppendChild(headerNode.OwnerDocument.ImportNode(child, true));
                }

                return doc;
            }
        }

        public byte[] DecryptOrderData(XPathHelper xph)
        {
            using (new MethodLogger(s_logger))
            {
                var encryptedOd = Convert.FromBase64String(xph.GetOrderData()?.Value);

                if (!Enum.TryParse<CryptVersion>(xph.GetEncryptionPubKeyDigestVersion()?.Value,
                    out var transKeyEncVersion))
                {
                    throw new DeserializationException(
                        string.Format("Encryption version {0} not supported",
                            xph.GetEncryptionPubKeyDigestVersion()?.Value), xph.Xml);
                }

                var encryptionPubKeyDigest = Convert.FromBase64String(xph.GetEncryptionPubKeyDigest()?.Value);
                var encryptedTransKey = Convert.FromBase64String(xph.GetTransactionKey()?.Value);

                var transKey = DecryptRsa(encryptedTransKey);
                var decryptedOd = DecryptAES(encryptedOd, transKey);

                if (!StructuralComparisons.StructuralEqualityComparer.Equals(Config.User.CryptKeys.Digest,
                    encryptionPubKeyDigest))
                {
                    throw new DeserializationException("Wrong digest in xml", xph.Xml);
                }

                return decryptedOd;
            }
        }

        public byte[] DecryptRsa(byte[] ciphertext)
        {
            using (new MethodLogger(s_logger))
            {
                var rsa = Config.User.CryptKeys.PrivateKey;
                return rsa.Decrypt(ciphertext, RSAEncryptionPadding.Pkcs1);
            }
        }

        public byte[] DecryptAES(byte[] ciphertext, byte[] transactionKey)
        {
            using (new MethodLogger(s_logger))
            {
                using (var aesAlg = Aes.Create())
                {
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.ANSIX923;
                    aesAlg.IV = new byte[16];
                    aesAlg.Key = transactionKey;
                    try
                    {
                        using (var decryptor = aesAlg.CreateDecryptor())
                        {
                            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                        }
                    }
                    catch (CryptographicException e)
                    {
                        aesAlg.Padding = PaddingMode.ISO10126;
                        using (var decryptor = aesAlg.CreateDecryptor())
                        {
                            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                        }
                    }
                }
            }
        }
    }
}
