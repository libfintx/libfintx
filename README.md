<img src="res/logo.png" align="right">

# libfintx

[![Build & Test](https://github.com/libfintx/libfintx/actions/workflows/dotnet.yml/badge.svg)](https://github.com/libfintx/libfintx/actions/workflows/dotnet.yml)
[![NuGet version (libfintx.FinTS)](https://img.shields.io/nuget/v/libfintx.FinTS.svg)](https://www.nuget.org/packages/libfintx.FinTS/)
[![License: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](https://www.gnu.org/licenses/lgpl-3.0)

A .NET client library for **HBCI 2.2**, **FinTS 3.0**, **EBICS H004** and **EBICS H005**.

In 1995 the ZKA announced a common online banking standard called *Homebanking Computer Interface* (HBCI). In 2003 they published the next generation of this protocol standard and named it *Financial Transaction Services* (FinTS). Today most German banks support these online banking standards.

The Electronic Banking Internet Communication Standard (EBICS) is a German transmission protocol developed by the German Banking Industry Committee for sending payment information between banks over the Internet. It is mandated for use by German banks and has also been adopted by France and Switzerland. [Wikipedia](https://en.wikipedia.org/wiki/Electronic_Banking_Internet_Communication_Standard).

With libfintx you can read the balance of a bank account, download account statements (MT940, camt.052/053) and securities holdings (MT535), and send SEPA payments, using **PIN/TAN** (including decoupled approval in the banking app) or **EBICS**.

# Features

* **FinTS / HBCI**: accounts, balance, statements, holdings, transfers, scheduled transfers, direct debits and banker's orders, with strong customer authentication. See [docs/FinTS.md](docs/FinTS.md).
* **EBICS**: INI/HIA/HPB initialization, credit transfers (CCT), direct debits (CDD) and statements (STA, C52, C53). See [docs/EBICS.md](docs/EBICS.md).

# Packages

The library is available as NuGet packages. All packages share one version.

| Package | Description |
| ------- | ----------- |
| [libfintx.FinTS](https://www.nuget.org/packages/libfintx.FinTS/) | FinTS/HBCI banking client |
| [libfintx.EBICS](https://www.nuget.org/packages/libfintx.EBICS/) | EBICS banking client |
| [libfintx.Swift](https://www.nuget.org/packages/libfintx.Swift/) | SWIFT MT940 and MT535 parsing |
| [libfintx.Sepa](https://www.nuget.org/packages/libfintx.Sepa/) | SEPA pain messages |

The other packages (`libfintx.Globals`, `libfintx.Logger`, `libfintx.Security`, `libfintx.EBICSConfig`, `libfintx.StatePrinter`, `libfintx.Xml`, `libfintx.Xsd`) are dependencies of these.

See the [changelog](CHANGELOG.md) for what changed in each version.

# Target platforms

* .NET 6, .NET 8 and .NET 10 (FinTS, EBICS)
* .NET Standard 2.0 (FinTS)

# Samples

Sample code is in [docs/FinTS.md](docs/FinTS.md#sample-code) and [docs/EBICS.md](docs/EBICS.md#sample-code). Demo projects: [libfintx.Sample](src/libfintx.Sample) and [libfintx.Sample.Ui](src/libfintx.Sample.Ui).

# Tested banks

* Raiffeisenbanken
* Sparkassen
* DKB
* DiBa
* Consorsbank
* Sparda
* Postbank
* Norisbank
* Deutsche Bank
* Unicredit Bank
* Commerzbank

# Copyright & License

Copyright (c) 2016 - 2026 **Torsten Klinger**

Licensed under **GNU LESSER GENERAL PUBLIC LICENSE Version 3, 29 June 2007**. Please read the LICENSE file.

# Support

You can contact me via [E-Mail](mailto:torsten.klinger@googlemail.com).
