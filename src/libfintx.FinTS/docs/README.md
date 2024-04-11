# libfintx.FinTS
[![License: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](https://www.gnu.org/licenses/lgpl-3.0)
[![Slack Status](https://img.shields.io/badge/slack-join_chat-white.svg?logo=slack&style=social)](https://libfintx.slack.com)

An C# based client library for **HBCI 2.2**, **FinTS 3.0**.

In 1995 the ZKA announced a common online banking standard called *Homebanking Computer Interface* (HBCI). In 2003 they published the next generation of this protocol standard and named it *Financial Transaction Services* (FinTS).

Today most of all german banks support this online banking standards.

It can be used to read the balance of a bank account, receive an account statement, and make a SEPA payment using **PIN/TAN**.

# Features

* Get Balance (**HKSAL**)
* Request Transactions (**HKKAZ**)
* Transfer money (**HKCCS**)
* Transfer money at a certain time (**HKCCS**)
* Collective transfer money (**HKCCM**)
* Collective transfer money terminated (**HKCME**)
* Rebook money from one to another account (**HKCUM**)
* Collect money (**HKDSE**)
* Collective collect money (**HKDME**)
* Load mobile phone prepaid card (**HKPPD**)
* Submit banker's order (**HKCDE**)
* Get banker's orders (**HKCSB**)
* Send Credit Transfer Initiation (**CCT**)
* Send Direct Debit Initiation (**CDD**)
* Pick up Swift daily statements (**STA**)

# Specification

For exact information please refer to the [german version of the specification](http://www.hbci-zka.de/spec/spezifikation.htm).

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

# Sample code (FinTS)

Check account balance.

```csharp
/// <summary>
/// Kontostand abfragen
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>
private async void btn_kontostand_abfragen_Click(object sender, EventArgs e)
{
    var connectionDetails = GetConnectionDetails();
    var client = new FinTsClient(connectionDetails);
    var sync = await client.Synchronization();

    HBCIOutput(sync.Messages);

    if (sync.IsSuccess)
    {
        // TAN-Verfahren
        client.HIRMS = txt_tanverfahren.Text;

        if (!await InitTANMedium(client))
            return;

        var balance = await client.Balance(_tanDialog);

        HBCIOutput(balance.Messages);

        if (balance.IsSuccess)
            SimpleOutput("Kontostand: " + Convert.ToString(balance.Data.Balance));
    }
}
```

# Community

Join our community on [Slack](https://libfintx.slack.com).

# Copyright & License

Licensed under **GNU LESSER GENERAL PUBLIC LICENSE Version 3, 29 June 2007**. Please read the [LICENSE](https://github.com/libfintx/libfintx/blob/main/LICENSE) file.

Copyright (c) 2016 - 2024 **Torsten Klinger**
