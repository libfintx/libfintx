using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using libfintx.EBICS;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Xunit;

namespace libfintx.Tests;

public class Test_EbicsKeyUtils : IDisposable
{
    private const string Password = "test-password";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "libfintx-ebics-" + Guid.NewGuid().ToString("N"));

    public Test_EbicsKeyUtils() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, true);

    [Theory]
    [InlineData(KeyUtils.KeyType.Signature, X509KeyUsageFlags.NonRepudiation)]
    [InlineData(KeyUtils.KeyType.Encryption, X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.KeyAgreement)]
    [InlineData(KeyUtils.KeyType.Authentication, X509KeyUsageFlags.DigitalSignature)]
    public void GeneratesACertificateThatLoadsFromPkcs12(KeyUtils.KeyType type, X509KeyUsageFlags usage)
    {
        var file = Path.Combine(_directory, type.ToString());

        var generated = KeyUtils.Generate(file, type, "user@example.com", Password);

        using var cert = KeyUtils.ReadCert(file + ".p12", Password);
        Assert.True(cert.HasPrivateKey);
        Assert.Equal("CN=user@example.com", cert.Subject);
        Assert.Equal(generated.SerialNumber.ToByteArrayUnsigned(), cert.GetSerialNumber().Reverse().ToArray());
        Assert.Equal(usage, cert.Extensions.OfType<X509KeyUsageExtension>().Single().KeyUsages);

        // The subject key identifier is the SHA-1 of the public key, and the authority key identifier repeats it.
        var ski = cert.Extensions.OfType<X509SubjectKeyIdentifierExtension>().Single().SubjectKeyIdentifier;
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(cert.PublicKey.EncodedKeyValue.RawData)), ski);
        var aki = cert.Extensions.Single(e => e.Oid?.Value == "2.5.29.35").RawData;
        Assert.Contains(ski, Convert.ToHexString(aki));
    }

    [Fact]
    public void WritesTheKeyPairAsEncryptedPem()
    {
        var file = Path.Combine(_directory, "sign");

        var generated = KeyUtils.Generate(file, KeyUtils.KeyType.Signature, "user@example.com", Password);

        using var reader = new StreamReader(file + ".key");
        var keyPair = (AsymmetricCipherKeyPair) new PemReader(reader, new StaticPassword(Password)).ReadObject();
        Assert.Equal(generated.GetPublicKey(), keyPair.Public);
    }

    private sealed class StaticPassword : IPasswordFinder
    {
        private readonly string _password;

        public StaticPassword(string password) => _password = password;

        public char[] GetPassword() => _password.ToCharArray();
    }
}
