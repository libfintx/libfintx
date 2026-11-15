using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using libfintx.EBICSConfig;

namespace libfintx.EBICS.Version3
{
    public sealed partial class Ebics3Client
    {
        /// <summary>Certificate-based H005 letter text. The bank's activation process and identity checks still apply.</summary>
        public IReadOnlyDictionary<string, string> CreateInitializationLetters(string bankName, string subscriberName)
        {
            string Letter(string role, string version, X509Certificate2 cert)
            {
                if (cert == null) throw new ArgumentException("All three H005 certificates are required.");
                using var key = cert.GetRSAPublicKey();
                return new StringBuilder().AppendLine("EBICS 3 / H005 initialisation")
                    .AppendLine("Bank: " + bankName).AppendLine("Host ID: " + config.User.HostId)
                    .AppendLine("Partner ID: " + config.User.PartnerId).AppendLine("User ID: " + config.User.UserId)
                    .AppendLine("Subscriber: " + subscriberName).AppendLine("Role: " + role)
                    .AppendLine("Algorithm: " + version).AppendLine("RSA bits: " + key.KeySize)
                    .AppendLine("Certificate SHA-256 (DER): " + Convert.ToHexString(Ebics3Crypto.CertificateDigest(cert)))
                    .AppendLine("Certificate (Base64 DER):").AppendLine(Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks))
                    .AppendLine().AppendLine("Place/date: ____________________")
                    .AppendLine("Authorised signature: ____________________").ToString();
            }
            return new Dictionary<string,string>
            {
                ["INI"] = Letter("Electronic signature",config.User.SignKeys.Version.ToString(),config.User.SignKeys.Certificate),
                ["HIA-X002"] = Letter("Authentication","X002",config.User.AuthKeys.Certificate),
                ["HIA-E002"] = Letter("Encryption","E002",config.User.CryptKeys.Certificate)
            };
        }
    }
}
