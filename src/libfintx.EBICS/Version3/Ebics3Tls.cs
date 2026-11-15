using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace libfintx.EBICS.Version3
{
    /// <summary>TLS/KMS annex (2026): TLS 1.2+, server authentication and dedicated bank trust anchors.</summary>
    public static class Ebics3Tls
    {
        private static readonly TlsCipherSuite[] ForwardSecretSuites =
        {
            TlsCipherSuite.TLS_AES_256_GCM_SHA384, TlsCipherSuite.TLS_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384, TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384, TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256, TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
            TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384, TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256
        };

        /// <summary>Dedicated anchors replace the generic OS roots for this handler only.
        /// Linux defaults to forward-secret AEAD suites. On other platforms configure the OS cipher policy.
        /// Explicit suite policies fail on unsupported platforms, rather than silently ignoring them.</summary>
        public static SocketsHttpHandler CreateHandler(X509Certificate2Collection dedicatedTrustAnchors = null,
            IEnumerable<TlsCipherSuite> allowedCipherSuites = null)
        {
            var ssl = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.Online,
                RemoteCertificateValidationCallback = (_, certificate, _, errors) =>
                    errors == SslPolicyErrors.None && certificate is X509Certificate2 cert && cert.Version == 3
            };
            if (dedicatedTrustAnchors != null)
            {
                if (dedicatedTrustAnchors.Count == 0) throw new ArgumentException("At least one dedicated trust anchor is required.");
                var policy = new X509ChainPolicy { TrustMode = X509ChainTrustMode.CustomRootTrust,
                    RevocationMode = X509RevocationMode.Online, VerificationFlags = X509VerificationFlags.NoFlag };
                policy.CustomTrustStore.AddRange(dedicatedTrustAnchors);
                policy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1")); // TLS server authentication
#if NET8_0_OR_GREATER
                ssl.CertificateChainPolicy = policy;
#else
                // .NET 6 has no SslClientAuthenticationOptions.CertificateChainPolicy.
                // Replace only chain trust; retain the TLS stack's name/presence checks.
                ssl.RemoteCertificateValidationCallback = (_, certificate, peerChain, errors) =>
                {
                    if ((errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None ||
                        certificate is not X509Certificate2 cert || cert.Version != 3) return false;
                    using var chain = new X509Chain();
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.AddRange(policy.CustomTrustStore);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));
                    if (peerChain != null)
                        foreach (var element in peerChain.ChainElements.Cast<X509ChainElement>().Skip(1))
                            chain.ChainPolicy.ExtraStore.Add(element.Certificate);
                    return chain.Build(cert);
                };
#endif
            }
            var suites = allowedCipherSuites?.ToArray() ?? (OperatingSystem.IsLinux() ? ForwardSecretSuites : null);
            if (suites != null)
            {
                if (suites.Length == 0 || suites.Any(s => !ForwardSecretSuites.Contains(s)))
                    throw new ArgumentException("Use a nonempty subset of the supported forward-secret AEAD cipher suites.");
                if (!OperatingSystem.IsLinux())
                    throw new PlatformNotSupportedException("Configure the operating system TLS cipher policy on this platform.");
                ssl.CipherSuitesPolicy = new CipherSuitesPolicy(suites);
            }
            return new SocketsHttpHandler { SslOptions = ssl, AllowAutoRedirect = false,
                UseCookies = false, PooledConnectionLifetime = TimeSpan.FromMinutes(5) };
        }
    }
}
