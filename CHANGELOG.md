# Changelog

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
