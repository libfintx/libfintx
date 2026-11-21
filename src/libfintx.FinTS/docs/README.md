# libfintx.FinTS
[![License: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](https://www.gnu.org/licenses/lgpl-3.0)

A .NET client library for **HBCI 2.2** and **FinTS 3.0** with **PIN/TAN**.

In 1995 the ZKA announced a common online banking standard called *Homebanking Computer Interface* (HBCI). In 2003 they published the next generation of this protocol standard and named it *Financial Transaction Services* (FinTS). Today most German banks support these online banking standards.

# Features

* Accounts, balance (**HKSAL**) and transactions as MT940 (**HKKAZ**) or camt.052/053 (**HKCAZ**)
* Securities holdings as MT535 (**HKWPD**)
* Transfers, scheduled transfers, collective transfers, direct debits and banker's orders
* Strong customer authentication: TAN process 2, decoupled approval in the banking app (HKTAN#7), chipTAN flicker code and photoTAN
* Cached bank and user parameter data, stored where you choose (`IBpdStore`, `IUpdStore`)

The full list of features, the segments behind them and sample code are in [docs/FinTS.md](https://github.com/libfintx/libfintx/blob/main/docs/FinTS.md).

For the specification see the [FinTS documents of the Deutsche Kreditwirtschaft](https://www.hbci-zka.de/spec/spezifikation.htm).

# Copyright & License

Licensed under **GNU LESSER GENERAL PUBLIC LICENSE Version 3, 29 June 2007**. Please read the [LICENSE](https://github.com/libfintx/libfintx/blob/main/LICENSE) file.

Copyright (c) 2016 - 2026 **Torsten Klinger**
