# Changelog

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
