# EBICS H005 implementation and release gate

Status: EBICS 3.0.2 client implementation reviewed against the current normative documents; bank interoperability and deployment acceptance remain open. This is not a production certification.
Base repository commit: `63779020a08d7ec0c9544557e3c1298f736defc7`.

## Integration

`EbicsClient.Factory().Create(config)` selects the H005 transport when
`config.Version = EbicsVersion.H005`. Use `client.Version3` for the async protocol API.
H004 remains on its existing transport. Dispose clients after use.

```csharp
config.Version = EbicsVersion.H005;
using var client = new Ebics3Client(config);
// Obtain BOTH fingerprints independently from the bank before HPB.
var bank = await client.HPBAsync(bankAuthenticationSha256, bankEncryptionSha256);
config.Bank = bank.Bank; // only after successful fingerprint verification
var statement = await client.BTDAsync(new BtdRequest {
    Service = new BtfService {
        ServiceName = "EOP", Scope = "DE", ContainerType = "ZIP",
        MessageName = "camt.053", MessageVersion = "08"
    }
});
// The service above is illustrative: use the target bank's advertised BTF tuple.
// statement.Data retains original bytes, including any ZIP/XML/SVC container.
// Check IsSuccess AND ReceiptAcknowledged before treating a download as completed.
```

`BTUAsync` accepts arbitrary original payload bytes and a BTF service, including
message variant/version/format, service option and container. The library does not
silently change the payload's ISO 20022 version. Existing CCT/CDD builders generate
their existing pain versions; their H005 wrappers require an explicit matching
`BtfService`. For newer payment formats submit a correctly generated document via
BTU. Current BTF options such as VOI/VOO/VOP remain caller supplied; the transport
does not select a verification-of-payee policy on the caller's behalf. The standardised EBICS transport and the national payment-format lifecycle
are separate contracts.

Existing STA/C53 wrappers require `Params.BtfService`; use BTD directly to preserve
binary files and separate ZIP entries. INI/HIA/SPR route to H005 automatically.
PTK is not an H005 order: its legacy wrapper throws in H005 mode. Use
`Version3.HACAsync()` and `HacAcknowledgement.Parse` for pain.002.001.03.
For the legacy HPB wrapper, Config.Bank must already contain independently trusted
certificates. First-time provisioning uses the explicit fingerprint API above.

## Implemented protocol surface

- BTD and BTU, binary payloads, optional date range, file name, generic parameters,
  electronic-signature flag and distributed-signature request flag.
- Complete caller-selected signer set for BTU, including mixed A005/A006 signatures
  and multiple ES in one upload (e.g. French TS double-signature profiles).
- Configured technical SystemID and security-medium identifier.
- Sequential initialisation, encrypted transfer segments, receipt; bounded payload
  sizes and segment count, transaction/phase/sequence checks.
- INI/HIA, certificate-based initialisation-letter text, H3K (requires bank-agreed
  CA-issued certificates), HPB with independently pinned certificate fingerprints.
- HCA/HCS/PUB key replacement: sign with current keys, transmit new certificates,
  install new keys only after successful acknowledgement. No automatic key replacement.
- Explicit `AllowMigratedUserKeys` mode for already activated 2.x user keys, per
  CR EB-20-03/04. Missing certificates never trigger an implicit fallback. New
  INI/HIA/H3K and key replacements still require certificates.
- HEV discovery without automatic downgrade; HPD/HAA/HAC/HKD/HTD downloads.
- HAC protocol-step parsing, separate positive/negative 3.0.2 final indications,
  and explicit recognition of the legacy neutral label. Positive EBICS completion
  includes authorised cancellation; it does not prove payment settlement.
- HVU/HVZ overview, HVD state, HVT details, HVE authorisation and HVS cancellation.
  HVE/HVS require the digest of the already-reviewed order, never a guessed digest.
- Administrative parameter XML is schema-checked. Download payloads are returned as
  raw bytes; applications may deserialize the existing `libfintx.Xsd.H005` types.

## Security requirement review

| Area | Previous path | H005 candidate | Evidence/check |
|---|---|---|---|
| Protocol XML | H004-shaped commands under H005 namespace | AdminOrderType; no OrderAttribute; BTF and mandatory DataDigest | Embedded H005 XSD validation on requests and responses |
| Key representation | RSA modulus/exponent | X.509v3 DER certificates with validity, RSA size and role-specific KeyUsage checks; self-signed certificate factory | INI/HIA/H3K schemas; Common IG 3.1; expired/wrong-role tests |
| Key digest | German exponent/modulus hash | SHA-256 over full DER certificate | Specification 3.0 sections 4 and 5; HPB pin rejection tests |
| E002 order encryption | AES CBC plus ad-hoc retry | AES-128-CBC, zero IV, ANSI X9.23 output; ISO10126-compatible input padding | Specification 3.0 section 11.3.2; binary multisegment round trips |
| E002 key transport | RSA PKCS#1 v1.5 | RSA PKCS#1 v1.5, fresh 16-byte CSPRNG transaction key | Same normative E002 algorithm; no invented OAEP/GCM switch |
| A006 | Custom modular exponentiation, fixed 2048-bit encoding and private-key export | .NET RSA-PSS with SHA-256 over the order digest (double hashing), 2048–4096-bit keys | Section 14.1.4.2.5; tests at 2048/3072/4096 bits |
| A005 | Legacy path | Retained as an explicit standard option, no silent fallback | SHA-256 / PKCS#1 signature over order digest |
| Order digest | Payload-specific whitespace removal | Remove CR, LF and Ctrl-Z only; retain tabs and original transport bytes | Section 14; normalisation test |
| X002 | Missing verification in download commands | Verify RSA-SHA256, strict algorithm/reference structure and digest before use; accept omitted leading RSA zero octets | Independent lxml/OpenSSL vectors, inherited namespaces/xml attributes, shortened integer, tamper tests |
| HPB trust | Keys returned without trust establishment | Match BOTH out-of-band certificate fingerprints; locate pinned leaf among unordered certificate elements | Wrong authentication/encryption pin tests; unsigned key-management response as specified |
| TLS | Default HTTP client | HTTPS only; TLS 1.2/1.3, X.509v3, chain/name/revocation checks; no redirects; Linux forward-secret AEAD suite policy; optional dedicated bank roots | 2026 TLS/KMS annex; factory policy tests; injected HttpClient is caller-controlled |
| Key storage | Public key PEM loses certificate | Preserve public DER certificate; keep legacy PEM load/save | Persistence round trips, no private key saved |
| Protected key operations | Exported RSA private parameters | New H005 path calls RSA provider directly | Can use non-exportable provider keys; deployment must supply protected storage |
| XML security | Permissive parsing | No DTD/external resolver; local schemas; validation on a copy preserves physically transmitted signature inputs; bounded streaming responses with timeout | Common IG 2.3.6; outer messages and key/signature payload schemas |
| Failures | Missing values could become success | Required six-digit codes; receipt requires 011000/000000 in Receipt phase; no automatic payment replay; optional durable persistence before receipt | Current return-code annex; false acknowledgements, corrupt payload and persistence-failure tests |
| Dependencies | System.Security.Cryptography.Xml 8.0.0 | 8.0.5 | NuGet restore/audit output; audit again at release |

## TLS and protected keys

`Ebics3Tls.CreateHandler(bankRoots)` creates a handler scoped to the independently
obtained bank trust anchors. Omitting roots uses platform trust. Both paths retain
TLS hostname, chain, validity and revocation checks. Linux defaults to a forward-secret
AEAD suite list; an explicit subset can implement bank-specific policy. On Windows
and other platforms the OS must enforce the negotiated cipher-suite policy; an
explicit unsupported per-handler policy throws instead of being ignored.
`Config.Insecure` cannot disable these H005 checks. An injected HttpClient must
provide equivalent controls. The bank URI is fixed when the client is created.

The March 2026 annex recommends appropriately certified authorisation keys or key
stores for EBICS 2.x/3.x; it plans a mandatory requirement for EBICS 4.0. It does not
change E002 to OAEP or GCM. Configure a provider-backed `RSA` in
`User.SignKeys.PrivateKey` / `AuthKeys.PrivateKey` / `CryptKeys.PrivateKey` **after**
setting the public certificate. H005 calls provider signing/decryption operations,
without exporting private parameters. A contract test rejects every key export and
still signs an upload. This does not certify a particular HSM/PKCS#11 adapter or
key store: the deploying application selects and operates that provider.

## Receipt and persistence

A successful decode alone cannot guarantee durable application storage. For downloads
that must be persisted before the bank marks them collected, use `BeforeReceipt`:

```csharp
var request = new BtdRequest {
    Service = bankAdvertisedService,
    BeforeReceipt = async (download, ct) => {
        // Use a durable, idempotent store; TransactionId identifies this transfer.
        await store.SaveAsync(download.TransactionId, download.Data, ct);
    }
};
var result = await client.BTDAsync(request);
if (!result.IsSuccess || !result.ReceiptAcknowledged) {
    // Inspect codes and existing stored data. Do not blindly replay an upload.
}
```

The same hook is available for HAC and administrative downloads. Decode/persistence
exceptions cause a best-effort negative receipt; the original exception is preserved.
Without a callback the library acknowledges successful in-memory decoding. A network
failure after persistence or after sending a receipt can leave the outcome uncertain;
retain the transaction identifier and reconcile before any business retry.

## Current-source review (2026-11-15)

The public download terms were accepted with the user's authorisation. The following
originals were obtained directly from EBICS.org and reviewed. Documents are not
redistributed in this repository; retrieve them from the publisher under its terms.

Source indexes:

- https://www.ebics.org/en/technical-information/ebics-specification
- https://www.ebics.org/en/technical-information/implementation-guide
- https://www.ebics.org/en/technical-information/archive-ebics/change-requests

| Original | SHA-256 |
|---|---|
| 2022-06-27 EBICS 3.0.2 detailed specification | `f12bd46e3afefef66d64838d221e96ebabd1bf579ef15d3a92e8524d43636b3c` |
| 2022-06-27 Return Codes annex | `5f6e4b2f273f4626f4222cef903abffa30e1fd64beb2acd820d4244c9cda008a` |
| 2026-03-20 TLS and KMS annex | `d8537a567a87500db476865929dfc086b0a47662462a91869fedaf0ed58b7cb3` |
| 2022-06-27 Common Implementation Guide, extension 03 | `c8715987e78329bf2babf128d74ae7b8dea5559cee84b4a5d21212c19dd8e43f` |
| 2024-10-23 BTF External Code List archive | `33e4fe2cb75f2c6182d9e8c46c9b8252b37f8f91d46cde8e1a64845292d4dee9` |

All 13 change requests in the official 3.0.1 and 3.0.2 archives were included in
the review. The normative changes relevant to this client are recorded below.

| Requirement / clarification | Implementation consequence |
|---|---|
| CR EB-18-05, 3.0.2 8.3.4.1 / 8.3.5.1 | HVE/HVS carry empty DataDigest, while SignatureData signs the original reviewed digest; NumSegments is zero |
| CR EB-20-03 / EB-20-04 | Explicit reuse of previously activated raw user keys; legacy recipient-key digest only in this mode |
| CR EB-20-01 | HAC VEU_VERIFICATION_END requires DS05 |
| CR EB-22-03 | HAC final positive/negative labels; cancellation may produce positive EBICS completion |
| CR EB-18-03 | Manual pin verification remains available for bank-key changes; optional automatic predecessor-signed certificate rollover is not implemented |
| CR EB-21-01 | H3K may use a bank's private CA only within that bank's agreed trust domain; other banks require the appropriate INI/HIA process |
| CR EB-20-02 / EB-22-02; 2026 annex | TLS 1.2 minimum; current national/bank cipher policy and recommended forward secrecy; dedicated trust anchors |
| CR EB-22-01 | Bank agreement determines ES classes/count; explicit BTU signer lists support multiple signatures; SignatureFlag/requestEDS remain explicit; transport completion does not assert payment authorisation |
| CR EB-22-04 | Certificate factory accepts caller-supplied DN; it invents no mandatory personal subject attributes |
| CR EB-18-01 / EB-18-06 | BTF transport is not limited to German corporate orders; preserve numeric return codes without obsolete order-type naming |
| Common IG 2.3.6 | Validate a copy; do not inject implicit defaults into canonical signature input |
| BTF list, October 2024 | Open descriptor API accepts current codes; examples use EOP for camt.053, with bank-confirmed variants and scope |

## Remaining acceptance work

1. Interoperability tests against the customer's target banks: enrolment/activation,
   HPB trust, BTF services, multipart transfer, HAC, VEU, key rotation and negative
   receipts. No bank endpoint or bank credentials have been used in these tests.
2. Approve deployment TLS/CA policy and the chosen key provider, including certificate
   revocation, renewal, H3K trust domain and bank-approved initialisation-letter layout.
   The client validates certificate role, size and validity; manual HPB pinning is its
   supported trust path. Automatic CA-chain-based bank-key acceptance and predecessor
   rollover are not implemented.
3. Optional protocol recovery/resumption and prevalidation are not implemented.
   061101 remains a non-success result and uploads are never replayed automatically.
4. National business-format generation and XML/SVC container decoding remain separate
   from raw EBICS transport. Existing payment builders retain their stated ISO versions;
   pass other bank-approved payloads to BTU. HAC parsing is a protocol-step reader,
   not a general ISO 20022 schema validator or settlement-status engine.
5. Run additional independent bank fixtures and platform TLS integration tests. The
   .NET 6 dedicated-root compatibility branch has been compiled, but has not been
   exercised against a live TLS bank server. General FinTS regression tests are outside
   the isolated protocol harness used here.

## Validation commands

```sh
dotnet build src/libfintx.EBICS/libfintx.EBICS.csproj -m:1 -p:GeneratePackageOnBuild=false
dotnet build src/libfintx.EBICS.Tests -m:1 -p:TargetFrameworks=net10.0 -p:GeneratePackageOnBuild=false
dotnet src/libfintx.EBICS.Tests/bin/Debug/net10.0/libfintx.EBICS.Tests.dll
```

The executable test harness has no bank credentials, makes no real bank calls and
fails with a nonzero exit code on an assertion/exception. The independent X002
fixture generator uses lxml and Python cryptography; its private key is ephemeral
and never saved. The fixture certificate is public test data only.

### Observed local results (2026-11-15)

- EBICS project builds for net6.0, net8.0 and net10.0: zero errors.
- Protocol executable on net10.0: 121 passing assertions (including current-revision cases).
- Final restore/build reported no NU190x package vulnerability warnings for this
  dependency graph after the XML cryptography update.
- Compiler warnings remain, including existing legacy warnings and obsolete
  X509Certificate2 constructors on net10.0. No live-bank test was performed.
- The general FinTS test suite was not run; this change was validated with the
  isolated EBICS harness and all EBICS target-framework builds.
