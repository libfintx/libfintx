/*	
* 	
*  This file is part of libfintx.
*  
*  Copyright (C) 2018 Bjoern Kuensting
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
*  Updates done by Torsten Klement <torsten.klinger@googlemail.com>
*  
*  Updates Copyright (c) 2024 Torsten Klement
* 	
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Ionic.Zlib;
using Microsoft.Extensions.Logging;
using libfintx.EBICSConfig;
using libfintx.EBICS.Exceptions;
using libfintx.EBICS.Handler;
using libfintx.EBICS.Responses;
using ebics = libfintx.Xsd.H004;

namespace libfintx.EBICS.Commands
{
    internal abstract class Command
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<Command>();

        internal Config Config { get; set; }

        internal abstract string OrderType { get; }
        internal abstract string OrderAttribute { get; }
        internal abstract TransactionType TransactionType { get; }

        internal abstract IList<XmlDocument> Requests { get; }
        internal abstract XmlDocument InitRequest { get; }
        internal abstract XmlDocument ReceiptRequest { get; }

        protected static string S_digestAlg => "http://www.w3.org/2001/04/xmlenc#sha256";

        protected void UpdateResponse(Response resp, DeserializeResponse dr)
        {
            resp.BusinessReturnCode = dr.BusinessReturnCode;
            resp.TechnicalReturnCode = dr.TechnicalReturnCode;
            resp.ReportText = dr.ReportText;
        }

        protected T XMLDeserialize<T>(string payload, params Type[] types)
        {
            System.Xml.Serialization.XmlSerializer x =
                (types != null && types.Any())
                ? new System.Xml.Serialization.XmlSerializer(typeof(T), types)
                : new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (var reader = new StringReader(payload))
                return (T)x.Deserialize(reader);
        }
        string XMLSerialize<T>(T o, params Type[] types)
        {
            System.Xml.Serialization.XmlSerializer x =
                (types != null && types.Any())
                ? new System.Xml.Serialization.XmlSerializer(typeof(T), types)
                : new System.Xml.Serialization.XmlSerializer(typeof(T));
            var xw = new XmlWriterSettings
            {
                Encoding = System.Text.Encoding.UTF8,
                OmitXmlDeclaration = true,
                Indent = false
            };
            var sb = new StringBuilder();
            using (var writer = XmlWriter.Create(sb, xw))
                x.Serialize(writer, o);
            return sb.ToString();
        }
        protected XmlDocument ToDocument(string s)
        {
            var x = new XmlDocument() { PreserveWhitespace = true };
            x.LoadXml(s);
            var nm = new XmlNamespaceManager(x.NameTable);
            nm.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            foreach (XmlNode node in x.SelectNodes("//*[@xsi:type]", nm))
            {
                if (node.Name == "OrderParams")
                {
                    var newnode = x.CreateNode(XmlNodeType.Element, node.Attributes["xsi:type"].Value.Replace("Type", ""), node.NamespaceURI);
                    newnode.InnerXml = node.InnerXml;
                    node.ParentNode.ReplaceChild(newnode, node);
                }
                else
                    node.Attributes.RemoveNamedItem("xsi:type");
            }

            return x;
        }
        protected XmlDocument XMLSerializeToDocument<T>(T o, params Type[] types)
        {
            return ToDocument(XMLSerialize(o, types));
        }

        internal virtual DeserializeResponse Deserialize_ebicsKeyManagementResponse(string payload, out ebics.ebicsKeyManagementResponse ebr)
        {
            using (new MethodLogger(s_logger))
            {
                ebr = XMLDeserialize<ebics.ebicsKeyManagementResponse>(payload);
                int.TryParse(ebr.header.mutable.ReturnCode, out var techCode);
                int.TryParse(ebr.body.ReturnCode.Value, out var busCode);
                var dr = new DeserializeResponse
                {
                    BusinessReturnCode = busCode,
                    TechnicalReturnCode = techCode,
                    ReportText = ebr.header.mutable.ReportText
                };

                s_logger.LogDebug("DeserializeResponse: {response}", dr);
                return dr;
            }
        }
        internal abstract DeserializeResponse Deserialize(string payload);
        internal virtual DeserializeResponse Deserialize_ebicsResponse(string payload, out ebics.ebicsResponse ebr)
        {
            using (new MethodLogger(s_logger))
            {
                //do signature validation here
                var doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(payload);
                VerifySignature(doc, Config.Bank.AuthKeys.PublicKey);

                //var doc = XDocument.Parse(payload);
                ebr = XMLDeserialize<ebics.ebicsResponse>(payload);
                int.TryParse(ebr.header.mutable.ReturnCode, out var techCode);
                int.TryParse(ebr.body.ReturnCode.Value, out var busCode);
                int.TryParse(ebr.header.@static.NumSegments, out var numSegments);
                int.TryParse(ebr.header.mutable.SegmentNumber?.Value, out var segmentNo);
                var lastSegment = ebr.header.mutable.SegmentNumber?.lastSegment;
                Enum.TryParse<TransactionPhase>(ebr.header.mutable.TransactionPhase.ToString(), out var transPhase);

                var dr = new DeserializeResponse
                {
                    BusinessReturnCode = busCode,
                    TechnicalReturnCode = techCode,
                    NumSegments = numSegments,
                    SegmentNumber = segmentNo,
                    LastSegment = lastSegment ?? true,
                    TransactionId = ebr.header.@static.TransactionID==null?null:Convert.ToBase64String(ebr.header.@static.TransactionID),
                    Phase = transPhase,
                    ReportText = ebr.header.mutable.ReportText
                };

                s_logger.LogDebug("DeserializeResponse: {response}", dr);
                return dr;
            }
        }
        protected byte[] DecryptOrderData(ebics.ebicsKeyManagementResponseBodyDataTransfer dt)
        {
            using (new MethodLogger(s_logger))
            {
                var encryptedOd = dt.OrderData.Value;
                if (dt.DataEncryptionInfo.EncryptionPubKeyDigest.Algorithm != "http://www.w3.org/2001/04/xmlenc#sha256" ||
                    dt.DataEncryptionInfo.EncryptionPubKeyDigest.Version != "E002")
                    throw new DeserializationException("Encryption version {0} not supported");


                var encryptionPubKeyDigest = dt.DataEncryptionInfo.EncryptionPubKeyDigest.Value;
                var encryptedTransKey = dt.DataEncryptionInfo.TransactionKey;

                var transKey = DecryptRsa(encryptedTransKey);
                var decryptedOd = DecryptAES(encryptedOd, transKey);

                if (!StructuralComparisons.StructuralEqualityComparer.Equals(Config.User.CryptKeys.Digest,
                    encryptionPubKeyDigest))
                {
                    throw new DeserializationException("Wrong digest in xml");
                }

                return decryptedOd;
            }

        }

        protected DateTime ParseTimestamp(string dt)
        {
            DateTime.TryParseExact(dt, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var parsed);
            return parsed;
        }

        protected byte[] DecryptRsa(byte[] ciphertext)
        {
            using (new MethodLogger(s_logger))
            {
                var rsa = Config.User.CryptKeys.PrivateKey;
                return rsa.Decrypt(ciphertext, RSAEncryptionPadding.Pkcs1);
            }
        }

        protected byte[] EncryptRsa(byte[] ciphertext)
        {
            using (new MethodLogger(s_logger))
            {
                var rsa = Config.Bank.CryptKeys.PublicKey;
                return rsa.Encrypt(ciphertext, RSAEncryptionPadding.Pkcs1);
            }
        }

        protected byte[] EncryptAes(byte[] ciphertext, byte[] transactionKey)
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
                        using (var encryptor = aesAlg.CreateEncryptor())
                        {
                            return encryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                        }
                    }
                    catch (CryptographicException)
                    {
                        aesAlg.Padding = PaddingMode.ISO10126;
                        using (var encryptor = aesAlg.CreateEncryptor())
                        {
                            return encryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                        }
                    }
                }
            }
        }

        protected IList<string> Segment(string b64text)
        {
            using (new MethodLogger(s_logger))
            {
                var k = b64text.Length;
                var i = 0;

                var ret = new List<string>();

                while (true)
                {
                    if (k > 1024)
                    {
                        var partialStr = b64text.Substring(i, 1024);
                        ret.Add(partialStr);
                        i += 1024;
                        k -= 1024;
                    }
                    else
                    {
                        var partialStr = b64text.Substring(i);
                        ret.Add(partialStr);
                        break;
                    }
                }

                return ret;
            }
        }

        protected byte[] DecryptAES(byte[] ciphertext, byte[] transactionKey)
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
                    catch (CryptographicException)
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

        protected byte[] Decompress(byte[] buffer)
        {
            using (new MethodLogger(s_logger))
            {
                using (var output = new MemoryStream())
                {
                    using (var zs = new ZlibStream(output, CompressionMode.Decompress))
                    {
                        zs.Write(buffer, 0, buffer.Length);
                    }

                    return output.ToArray();
                }
            }
        }

        protected byte[] Compress(byte[] buffer)
        {
            using (new MethodLogger(s_logger))
            {
                using (var output = new MemoryStream())
                {
                    using (var zs = new ZlibStream(output, CompressionMode.Compress))
                    {
                        zs.Write(buffer, 0, buffer.Length);
                    }

                    return output.ToArray();
                }
            }
        }

        byte[] CanonicalizeAndDigest(XmlNodeList nodeList, bool dotraverser = true)
        {
            var encoding = System.Text.Encoding.UTF8;
            var transform = new XmlDsigC14NTransform();
            var outersb = new StringBuilder();
            foreach (XmlNode n in nodeList)
            {
                var newdoc = new XmlDocument { PreserveWhitespace = true };
                var newnode = newdoc.AppendChild(newdoc.ImportNode(n, true));
                var traverser = n.ParentNode;
                while (dotraverser && traverser != null)
                {
                    if (traverser.Attributes != null)
                        foreach (var a in traverser.Attributes.OfType<XmlAttribute>())
                        {
                            if (a.Name.StartsWith("xmlns"))
                            {
                                var newattrivute = (XmlAttribute)newdoc.ImportNode(a, true);
                                newnode.Attributes.Append(newattrivute);
                            }
                        }
                    traverser = traverser.ParentNode;
                }
                var sb = new StringBuilder();
                sb.Append(newnode.OuterXml);
                using (var transformerinput = new MemoryStream(encoding.GetBytes(sb.ToString())))
                {
                    transform.LoadInput(transformerinput);
                    using (var o = transform.GetOutput() as MemoryStream)
                        outersb.Append(encoding.GetString(o.ToArray()));
                }
            }
            return SHA256.Create().ComputeHash(encoding.GetBytes(outersb.ToString()));
        }

        protected void Sign(XmlDocument doc, RSA signKey)
        {
            const string _digestAlg = "http://www.w3.org/2001/04/xmlenc#sha256";
            const string _signAlg = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
            const string _canonAlg = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";

            const string DefaultReferenceUri = "#xpointer(//*[@authenticate='true'])";
            var nodeList = doc.SelectNodes("//*[@authenticate='true']");
            var digest = CanonicalizeAndDigest(nodeList);
            var signedInfo = new ebics.SignedInfoType
            {
                CanonicalizationMethod = new ebics.CanonicalizationMethodType
                {
                    Algorithm = _canonAlg
                },
                SignatureMethod = new ebics.SignatureMethodType
                {
                    Algorithm = _signAlg
                },
                Reference = new ebics.ReferenceType[]
                    {
                        new ebics.ReferenceType
                        {
                            DigestMethod=new ebics.DigestMethodType { Algorithm=_digestAlg},
                            DigestValue=digest,
                            Transforms=new ebics.TransformType[]
                            {
                                new ebics.TransformType{Algorithm=_canonAlg},
                            },
                            URI=DefaultReferenceUri
                        }
                    }
            };
            XmlNamespaceManager nm = new XmlNamespaceManager(doc.NameTable);
            nm.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
            var str = XMLSerializeToDocument(signedInfo);
            var _digest = CanonicalizeAndDigest(str.SelectNodes("//*[local-name()='SignedInfo']"));
            var signaturevalue = signKey.SignHash(_digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#if false
            var authsig = doc.SelectSingleNode("//*[local-name()='AuthSignature']");
            authsig.AppendChild(doc.ImportNode(str.DocumentElement,true));
            var sigval = doc.CreateNode(XmlNodeType.Element, "SignatureValue", "ds");
            sigval.AppendChild(doc.CreateTextNode(Convert.ToBase64String(signaturevalue)));
            authsig.AppendChild(sigval);
#else

            var signature = new ebics.SignatureType { SignedInfo = signedInfo };
            signature.SignatureValue = new ebics.SignatureValueType { Value = signaturevalue };
            var sigdoc = XMLSerializeToDocument(signature);
            var oldchild = doc.SelectSingleNode("//*[local-name()='AuthSignature']");
            if (oldchild != null)
            {
#if true
                var newChild = doc.ImportNode(sigdoc.DocumentElement, true);
                oldchild.ParentNode.ReplaceChild(newChild, oldchild);
#else
                foreach (XmlNode n in sigdoc.DocumentElement.ChildNodes)
                            {
                                oldchild.AppendChild(doc.ImportNode(n, true));
                            }
#endif
            }
            else throw new NotImplementedException();
#endif

        }
        protected void VerifySignature(XmlDocument doc, RSA signKey)
        {
            const string _digestAlg = "http://www.w3.org/2001/04/xmlenc#sha256";
            const string _signAlg = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
            const string _canonAlg = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";

            const string DefaultReferenceUri = "#xpointer(//*[@authenticate='true'])";
            var nodeList = doc.SelectNodes("//*[@authenticate='true']");
            var digest = CanonicalizeAndDigest(nodeList);
            var signatureXml = doc.SelectSingleNode("//*[local-name()='AuthSignature']");
            var xmlserializer = new System.Xml.Serialization.XmlSerializer(typeof(ebics.SignatureType));
            ebics.SignatureType signature;
            using (var sr = new StringReader(signatureXml.OuterXml))
                signature = (ebics.SignatureType)xmlserializer.Deserialize(sr);
            if (signature.SignedInfo.CanonicalizationMethod.Algorithm != _canonAlg)
                throw new Exception("invalid canon alg");
            if (signature.SignedInfo.SignatureMethod.Algorithm != _signAlg)
                throw new Exception("invalid sig alg");
            var reference = signature.SignedInfo.Reference.Single();
            if (reference.URI != DefaultReferenceUri)
                throw new Exception("reference uri");
            if (reference.DigestMethod.Algorithm != _digestAlg)
                throw new Exception("invalid digest alg");
            var transform = reference.Transforms.Single();
            if (transform.Algorithm != _canonAlg)
                throw new Exception("invalid transform");
            if (reference.DigestValue.SequenceEqual(digest) == false)
                throw new Exception("invalid digest");
            var _digest = CanonicalizeAndDigest(signatureXml.SelectNodes("//*[local-name()='SignedInfo']"), true);
            if (signKey.VerifyHash(_digest, signature.SignatureValue.Value, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1) == false)
                throw new Exception("invalid signature");
        }
        protected XmlDocument Authenticate(ebics.ebicsNoPubKeyDigestsRequest initReq)
        {
            initReq.AuthSignature = new ebics.SignatureType { SignatureValue = new ebics.SignatureValueType { } };
            var doc = XMLSerializeToDocument(initReq);
            Sign(doc, Config.User.AuthKeys.PrivateKey);
            return doc;
        }
        protected XmlDocument Authenticate(ebics.ebicsRequest initReq)
        {
            initReq.AuthSignature = new ebics.SignatureType { SignatureValue = new ebics.SignatureValueType { } };
            var doc = XMLSerializeToDocument(initReq);
            Sign(doc, Config.User.AuthKeys.PrivateKey);
            return doc;
        }
        protected XmlDocument Authenticate(ebics.ebicsRequest initReq, Type type)
        {
            initReq.AuthSignature = new ebics.SignatureType { SignatureValue = new ebics.SignatureValueType { } };
            var doc = XMLSerializeToDocument(initReq, type);
            Sign(doc, Config.User.AuthKeys.PrivateKey);
            return doc;
        }
        
        protected string FormatXml(XDocument doc)
        {
            var xmlStr = doc.ToString(SaveOptions.DisableFormatting);
            xmlStr = xmlStr.Replace("\n", "");
            xmlStr = xmlStr.Replace("\r", "");
            xmlStr = xmlStr.Replace("\t", "");
            return xmlStr;
        }
        protected XElement SignData(XDocument doc, UserParams up)
        {
            var xmlStr = FormatXml(doc);

            var userSigData = new ebics.UserSignatureDataSigBookType
            {
                Items = new object[] {
                        new ebics.OrderSignatureDataType
                        {
                            PartnerID = Config.User.PartnerId,
                            UserID = Config.User.UserId,
                        }
                }
            };
            return SignData(userSigData, Encoding.UTF8.GetBytes(xmlStr), Config.User.SignKeys);

        }
        protected string SerializeToString(object o, params Type[] types)
        {
            var sb = new StringBuilder();
            using (var sw = new Utf8StringWriter(sb))
            {
                System.Xml.Serialization.XmlSerializer xs = (types == null)
                    ? new System.Xml.Serialization.XmlSerializer(o.GetType())
                    : new System.Xml.Serialization.XmlSerializer(o.GetType(), types);
                xs.Serialize(sw, o);
                return sb.ToString();
            }
        }
        protected XDocument SerializeToDocument(object o, params Type[] types)
        {
            return XDocument.Parse(SerializeToString(o, types));
        }
        protected XElement SignData(ebics.UserSignatureDataSigBookType sd, byte[] data, SignKeyPair kp)
        {
            var sig = SignData(data, kp);
            var s = sd.Items.OfType<ebics.OrderSignatureDataType>().Last();
            s.SignatureValue = sig;
            s.SignatureVersion = kp.Version.ToString();
            var doc = SerializeToDocument(sd);
            return doc.Elements().First();
        }
        protected byte[] SignData(byte[] data, SignKeyPair kp)
        {
            switch (kp.Version)
            {
                case SignVersion.A005:
                    return kp.PrivateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                case SignVersion.A006:
                    return kp.PrivateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                default:
                    throw new CryptographicException("Signature version not supported");
            }
        }

        public override string ToString()
        {
            return
                $"{nameof(OrderType)}: {OrderType}, {nameof(OrderAttribute)}: {OrderAttribute}, {nameof(TransactionType)}: {TransactionType}";
        }
    }
}
