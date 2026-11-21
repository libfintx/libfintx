# libfintx.EBICS
[![License: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](https://www.gnu.org/licenses/lgpl-3.0)

A .NET client library for **EBICS H004** and **EBICS H005**.

The Electronic Banking Internet Communication Standard (EBICS) is a German transmission protocol developed by the German Banking Industry Committee for sending payment information between banks over the Internet. It is mandated for use by German banks and has also been adopted by France and Switzerland. [Wikipedia](https://en.wikipedia.org/wiki/Electronic_Banking_Internet_Communication_Standard).

# Features

* Initialization (**INI**, **HIA**, **HPB**) and the INI and HIA letters
* Credit transfers (**CCT**) and direct debits (**CDD**)
* Statements as SWIFT (**STA**) and camt.052/053 (**C52**, **C53**)
* Customer protocol (**PTK**) and suspending access (**SPR**)
* **E002** encryption, **X002** authentication, **A005** and **A006** signatures

The full list of features, the limitations and sample code are in [docs/EBICS.md](https://github.com/libfintx/libfintx/blob/main/docs/EBICS.md).

# Copyright & License

Licensed under **GNU LESSER GENERAL PUBLIC LICENSE Version 3, 29 June 2007**. Please read the [LICENSE](https://github.com/libfintx/libfintx/blob/main/LICENSE) file.

Copyright (c) 2016 - 2026 **Torsten Klinger**
