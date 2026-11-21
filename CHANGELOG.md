# Changelog

This project follows [Semantic Versioning](https://semver.org/): a major version for breaking changes, a minor version for new features and a patch version for bug fixes.

## Version 1.5.0 (2026-11-21)

- :tada: add securities statement (Depotaufstellung) via HKWPD and MT535: `FinTsClient.Holdings` and the new `MT535Parser` in `libfintx.Swift` ([#95](https://github.com/libfintx/libfintx/pull/95))
- :tada: add `EndDialog` to end the dialog of the last business transaction with HKEND ([#90](https://github.com/libfintx/libfintx/pull/90))
- :rocket: add `IUpdStore` to choose where the UPD are stored (`UpdFileStore` as default, `UpdInMemoryStore`), and use the stored BPD when a dialog starts without synchronization ([#87](https://github.com/libfintx/libfintx/pull/87))
- :rocket: use the decoupled status request parameters from HITANS#7 ([#83](https://github.com/libfintx/libfintx/pull/83))
- :rocket: add `AccountInformation.AccountKind` with the FinTS account kind from the UPD ([#95](https://github.com/libfintx/libfintx/pull/95))
- :rocket: add `HBCIDialogResult.IsAuthenticationPending` ([#88](https://github.com/libfintx/libfintx/pull/88))
- :rocket: add `CamtStatement.HasEndBalance`, false when a camt report has no booked balance (`EndBalance` is then 0) ([#94](https://github.com/libfintx/libfintx/pull/94))
- :rocket: add `CamtTransaction.UltimatePartnerName`, and prefer camt.052.001.08 when the bank offers both versions ([#94](https://github.com/libfintx/libfintx/pull/94))
- :rocket: *change* rename `Helper.EscapeHbciString` to `EscapeEdifactString` (the old name is obsolete)
- :rocket: *change* reference `Microsoft.Extensions.Logging(.Abstractions)` 10.0.14, so applications on .NET 10 build against libfintx ([#93](https://github.com/libfintx/libfintx/pull/93))
- :rocket: log an excerpt of a segment that cannot be parsed, and the whole segment at debug level ([#93](https://github.com/libfintx/libfintx/pull/93))
- :bug: *fix* read the camt.052.001.08 reports banks send without MT940 (no account currency, entries without transaction details or bank transaction code, interim booked balance as end balance) ([#94](https://github.com/libfintx/libfintx/pull/94))
- :bug: *fix* HKCAZ names the account from the UPD like HKKAZ does, and requests camt.052.001.08 when HICAZS names no format ([#94](https://github.com/libfintx/libfintx/pull/94))
- :bug: *fix* keep polling for a decoupled approval while the bank reports 3956 ([#86](https://github.com/libfintx/libfintx/pull/86))
- :bug: *fix* treat a pending strong customer authentication as no success, stop statement downloads while an approval is pending and accept warning-only answers to HKKAZ/HKCAZ ([#88](https://github.com/libfintx/libfintx/pull/88))
- :bug: *fix* stop polling for a decoupled approval when the caller cancels ([#81](https://github.com/libfintx/libfintx/pull/81))
- :bug: *fix* HKTAN process 2/S segments and the TAN in the signature ([#82](https://github.com/libfintx/libfintx/pull/82))
- :bug: *fix* read the chipTAN flicker code from the HHD_UC element and show the challenge text when it is malformed ([#80](https://github.com/libfintx/libfintx/pull/80))
- :bug: *fix* mask PIN, TAN and user id in the trace log by segment structure, and trace the bank's response instead of the request twice ([#89](https://github.com/libfintx/libfintx/pull/89))
- :bug: *fix* accept a segment header with an empty reference (`HIRMG:2:2:+...`) and lists of languages and HBCI versions in HIBPA ([#93](https://github.com/libfintx/libfintx/pull/93))
- :bug: *fix* take binary data (`@len@`) over whole when splitting data elements, so camt answers (HICAZ) are parsed ([#93](https://github.com/libfintx/libfintx/pull/93))
- :bug: *fix* send the actual cached BPD version in the INI dialog instead of a hardcoded 0
- :bug: *fix* encode outgoing FinTS messages and decode Base64 as ISO-8859-1 instead of ASCII
- :bug: *fix* escape FinTS syntax characters in the TAN medium name and PIN
- :bug: *fix* parse and format FinTS/SWIFT/SEPA amounts culture-independently
- :bug: *fix* reduce SEPA text to the SEPA charset before XML escaping
- :bug: *fix* log and skip malformed MT940 closing balances

## Version 1.4.0 (2026-06-09)

- :rocket: add .NET 10 to the target frameworks ([#64](https://github.com/libfintx/libfintx/pull/64))
- :rocket: complete EBICS C53 implementation and use Latin1 encoding
- :rocket: *change* repair and reunify the `main` branch onto the maintained line ([#67](https://github.com/libfintx/libfintx/pull/67))
- :bug: *fix* decoupled TAN (HKTAN#7) not completing when the bank sends both 0030 and 3955
- :bug: *fix* HKTAN omitted when the bank skips BPD due to an unchanged cached version
- :bug: *fix* HKKAZ account matching and harden UPD / segment parsing
- :bug: *fix* two crashes in HBCI synchronization (empty UserID/PIN redaction; malformed cached BPD)
- :bug: *fix* `BPD.Parse` crash on a null/empty cached BPD

## Version 1.3.0 (2025-01-03)

- :tada: redesign BPD handling: interface IBpdStore supports now use of different BPD storeand BPD version is now retrieved from cache and send when calling Synchronization
- :tada: *change* Use Microsoft.Extensions.Logging in FinTS ([#34](https://github.com/libfintx/libfintx/issues/34)
- :rocket: add FinTsVersion enumeration replacing class libfintx.FinTs.Version.HBCI
- :rocket: avoiding duplicated synchronization calls by keepting the client class active as long as the connection details do not change
- :rocket: *change* refactor message parsing ([#35](https://github.com/libfintx/libfintx/pull/35))

## Version 1.2.0 (2024-07-25)

- :tada: add strong name for NuGet projects ([#3](https://github.com/libfintx/libfintx/issues/3))
- :rocket: *change* move SWIFT message handling into separate library `libfintx.Swift`
- :rocket: *change* Properly parse HIRMG/HIRMS messages in bank code result (see [8c25a6d](https://github.com/libfintx/libfintx/commit/8c25a6d))
- :rocket: Update Commerzbank photo tan
- :rocket: make use of library SixLabors.ImageSharp optional
- :rocket: *change* rename `InputDate` to `EntryDate` in `SwiftTransaction` according to the MT940 specification
- :bug: *fix* HKTAB sending TAN medium type 0 ([#28](https://github.com/libfintx/libfintx/issues/28))

## Version 1.1.0 (2024-04-01)

- Bank database update: Switch FinTS URL fiducia.de to atruvia.de
- Add .NET 8.0 to the target frameworks
- upgrade depdencies (incl. security fixes)
- upgrade SixLabors.ImageSharp to 3.1.x (from 1.0.2/-beta)
- refactoring if MT940 parsing; functions renamed

## Version 1.0.0

*Versions with tag 1.0.0 do not have a changelog and are outdated. A upgrade to a higher version is strongly recommended.*
