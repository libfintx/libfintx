using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace libfintx.EBICS.Version3
{
    public enum Ebics3KeyRole { Signature, Authentication, Encryption }

    /// <summary>Common Implementation Guide certificate checks. Fingerprint/CA trust is a separate check.</summary>
    public static class Ebics3Certificates
    {
        public static void Validate(X509Certificate2 certificate, Ebics3KeyRole role)
        {
            if (certificate == null) throw new CryptographicException("An H005 certificate is required.");
            var now = DateTime.UtcNow;
            if (certificate.Version != 3 || now < certificate.NotBefore.ToUniversalTime() || now > certificate.NotAfter.ToUniversalTime())
                throw new CryptographicException("The H005 certificate must be X.509v3 and currently valid.");
            using var rsa = certificate.GetRSAPublicKey();
            if (rsa == null || rsa.KeySize < 2048 || rsa.KeySize > (role == Ebics3KeyRole.Signature ? 4096 : 16384))
                throw new CryptographicException("The certificate RSA key size is outside the EBICS range.");
            var usage = certificate.Extensions.OfType<X509KeyUsageExtension>().SingleOrDefault();
            var required = role switch
            {
                Ebics3KeyRole.Signature => X509KeyUsageFlags.NonRepudiation,
                Ebics3KeyRole.Authentication => X509KeyUsageFlags.DigitalSignature,
                Ebics3KeyRole.Encryption => X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.KeyAgreement,
                _ => throw new ArgumentOutOfRangeException(nameof(role))
            };
            if (usage == null || (usage.KeyUsages & required) == 0)
                throw new CryptographicException("The certificate KeyUsage does not permit its EBICS role.");
        }

        /// <summary>Create a role-specific certificate without exporting private key material.
        /// Agree validity dates with the bank (Common IG 3.1.1); generate a separate RSA key for each role.</summary>
        public static X509Certificate2 CreateSelfSigned(string subject, RSA key, Ebics3KeyRole role,
            DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var usage = role switch
            {
                Ebics3KeyRole.Signature => X509KeyUsageFlags.NonRepudiation,
                Ebics3KeyRole.Authentication => X509KeyUsageFlags.DigitalSignature,
                Ebics3KeyRole.Encryption => X509KeyUsageFlags.KeyEncipherment,
                _ => throw new ArgumentOutOfRangeException(nameof(role))
            };
            request.CertificateExtensions.Add(new X509KeyUsageExtension(usage, true));
            var subjectKey = new X509SubjectKeyIdentifierExtension(request.PublicKey, false);
            request.CertificateExtensions.Add(subjectKey);
            var identifier = Convert.FromHexString(subjectKey.SubjectKeyIdentifier);
            // DER AuthorityKeyIdentifier ::= SEQUENCE { keyIdentifier [0] OCTET STRING }.
            var authorityKey = new byte[identifier.Length + 4];
            authorityKey[0] = 0x30; authorityKey[1] = (byte)(identifier.Length + 2);
            authorityKey[2] = 0x80; authorityKey[3] = (byte)identifier.Length;
            identifier.CopyTo(authorityKey, 4);
            request.CertificateExtensions.Add(new X509Extension("2.5.29.35", authorityKey, false));
            var certificate = request.CreateSelfSigned(notBefore, notAfter);
            try { Validate(certificate, role); return certificate; }
            catch { certificate.Dispose(); throw; }
        }
    }
}
