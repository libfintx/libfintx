<img src="https://github.com/mrklintscher/libfintx/blob/master/res/logo.png" align="right">

# libfintx

[![Build & Test](https://github.com/libfintx/libfintx/actions/workflows/dotnet.yml/badge.svg)](https://github.com/libfintx/libfintx/actions/workflows/dotnet.yml)
[![NuGet version (libfintx.FinTS)](https://img.shields.io/nuget/v/libfintx.FinTS.svg)](https://www.nuget.org/packages/libfintx.FinTS/)
[![License: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](https://www.gnu.org/licenses/lgpl-3.0)

A .NET client library for **HBCI 2.2**, **FinTS 3.0**, **EBICS H004** and **EBICS H005**.

In 1995 the ZKA announced a common online banking standard called *Homebanking Computer Interface* (HBCI). In 2003 they published the next generation of this protocol standard and named it *Financial Transaction Services* (FinTS).

Today most of all german banks support this online banking standards.

The Electronic Banking Internet Communication Standard (EBICS) is a German transmission protocol developed by the German Banking Industry Committee for sending payment information between banks over the Internet. It grew out of the earlier BCS-FTAM protocol that was developed in 1995, with the aim of being able to use internet connections and TCP/IP. It is mandated for use by German banks and has also been adopted by France and Switzerland. [Wikipedia](https://en.wikipedia.org/wiki/Electronic_Banking_Internet_Communication_Standard).

This client library supports all four APIs, HBCI 2.2, FinTS 3.0 and EBICS H004 and H005.

It can be used to read the balance of a bank account, receive an account statement, and make a SEPA payment using **PIN/TAN** and **EBICS**.

# Usage

There are many reasons why you need to use a banking library which can exchange data from your application with the bank. One reason for example is to found a [Fintech](https://de.wikipedia.org/wiki/Finanztechnologie).

# Target platforms

* .NET 6.0+ (EBICS, FinTS)

# Sample

Look at the demo projects inside the master branch.

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
* EBICS **E002** for encryption
* EBICS **X002** for authentication
* EBICS **A005** and **A006** for signatures

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

# Sample code (EBICS)

## Initialization (INI/HIA)

The first thing you want to do as a new EBICS user is to announce your public RSA keys to your bank. You need to create three public/private key pairs for this (authentication, signature and encryption keys).

Creating the keys is easy with BouncyCastle.

```csharp
var gen = GeneratorUtilities.GetKeyPairGenerator("RSA");
gen.Init(new KeyGenerationParameters(new SecureRandom(), 4096));
var signKeyPair = gen.GenerateKeyPair();   

using (TextWriter sw = new StreamWriter("sign.key"))
{
    var pw = new PemWriter(sw);
    pw.WriteObject(signKeyPair);
    sw.Flush();
}
```

Adjust the above code and also create an authentication ("auth.key") and encryption ("enc.key") key.

Announce your public signature key to your bank. Note that the previously generated keys are stored in PEM format and contain the private and public key.

```csharp
AsymmetricCipherKeyPair signKey;

using (var sr = new StringReader(File.ReadAllText("sign.key").Trim()))
{
    var pr = new PemReader(sr);
    signKey = pr.ReadObject();
}

var signCert = KeyUtils.CreateX509Certificate2(signKey);

var client = EbicsClient.Factory().Create(new EbicsConfig
{
    Address = "The EBICS URL you got from your bank, i.e. https://ebics-server.com/",
    Insecure = true,
    TLS = true,
    User = new UserParams
    {
        HostId = "The host ID of your bank",
        PartnerId = "Your partner ID you got from your bank",
        UserId = "Your user ID you got from your bank",
        SignKeys = new SignKeyPair
        {
            Version = SignVersion.A005, // only A005 is supported right now
            TimeStamp = DateTime.Now,
            Certificate = signCert // internally we work with keys
        }
    }
});

var resp = c.INI(new IniParams());
```

After that we need to announce the public authentication and encryption keys.

```csharp
// loading of keys "auth.key" and "enc.key" omitted

var authCert = KeyUtils.CreateX509Certificate2(authKey);
var encCert = KeyUtils.CreateX509Certificate2(encKey);

var client = EbicsClient.Factory().Create(new EbicsConfig
{
    Address = "The EBICS URL you got from your bank, i.e. https://ebics-server.com/",
    Insecure = true,
    TLS = true,
    User = new UserParams
    {
        HostId = "The host ID of your bank",
        PartnerId = "Your partner ID",
        UserId = "Your user ID",
        AuthKeys = new AuthKeyPair
        {
            Version = AuthVersion.X002,
            TimeStamp = DateTime.Now,
            Certificate = authCert
        },
        CryptKeys = new CryptKeyPair
        {
            Version = CryptVersion.E002,
            TimeStamp = DateTime.Now,
            Certificate = encCert
        }
    }
});

var resp = c.HIA(new HiaParams());
```

Announcing the keys is not enough, as the bank needs to be sure that the keys really belong to you. To prove this, you need to send the INI and HIA letters to your bank. They contain hash values of your public keys and your written signature. The EBICS specification describes in detail how these letters should look like.

## Retrieving public bank keys (HPB)

In order to communicate via EBICS with the bank you need the bank's public keys, because data exchanged needs to be encrypted and authenticated.

```csharp
// loading of keys "auth.key" and "enc.key" omitted

var authCert = KeyUtils.CreateX509Certificate2(authKey);
var encCert = KeyUtils.CreateX509Certificate2(encKey);

var client = EbicsClient.Factory().Create(new EbicsConfig
{
    Address = "The EBICS URL you got from your bank, i.e. https://ebics-server.com/",
    Insecure = true,
    TLS = true,
    User = new UserParams
    {
        HostId = "The host ID of your bank",
        PartnerId = "Your partner ID",
        UserId = "Your user ID",
        AuthKeys = new AuthKeyPair
        {
            Version = AuthVersion.X002,
            TimeStamp = DateTime.Now,
            Certificate = authCert
        },
        CryptKeys = new CryptKeyPair
        {
            Version = CryptVersion.E002,
            TimeStamp = DateTime.Now,
            Certificate = encCert
        }
    }
});

var hpbResp = c.HPB(new HpbParams());
if (hpbResp.TechnicalReturnCode != 0 || hpbResp.BusinessReturnCode != 0)
{
    // handle error
    return;
}

c.Config.Bank = resp.Bank; // set bank's public keys

// now issue other commands 
```

## Direct credit transfer (CCT)

```csharp
// loading of keys "auth.key", "enc.key" and "sign.key" omitted

var authCert = KeyUtils.CreateX509Certificate2(authKey);
var encCert = KeyUtils.CreateX509Certificate2(encKey);
var signCert = KeyUtils.CreateX509Certificate2(signKey);

var client = EbicsClient.Factory().Create(new EbicsConfig
{
    Address = "The EBICS URL you got from your bank, i.e. https://ebics-server.com/",
    Insecure = true,
    TLS = true,
    User = new UserParams
    {
        HostId = "The host ID of your bank",
        PartnerId = "Your partner ID",
        UserId = "Your user ID",
        AuthKeys = new AuthKeyPair
        {
            Version = AuthVersion.X002,
            TimeStamp = DateTime.Now,
            Certificate = authCert
        },
        CryptKeys = new CryptKeyPair
        {
            Version = CryptVersion.E002,
            TimeStamp = DateTime.Now,
            Certificate = encCert
        },
        SignKeys = new SignKeyPair
        {
            Version = SignVersion.A005,
            TimeStamp = DateTime.Now,
            Certificate = signCert
        }
    }
});

var hpbResp = c.HPB(new HpbParams());
if (hpbResp.TechnicalReturnCode != 0 || hpbResp.BusinessReturnCode != 0)
{
    // handle error
    return;
}

c.Config.Bank = resp.Bank; // set bank's public keys

// create credit transfer data structure

var cctParams = new CctParams
{
    InitiatingParty = "Your name",
    PaymentInfos = new[]
    {
        new CreditTransferPaymentInfo
        {
            DebtorName = "Sender's name",
            DebtorAccount = "Sender's IBAN",
            DebtorAgent = "Sender's BIC",
            ExecutionDate = "2018-05-15",
            CreditTransferTransactionInfos = new[]
            {
                new CreditTransferTransactionInfo
                {
                    Amount = "1.00",
                    CreditorName = "Receiver's name",
                    CreditorAccount = "Receiver's IBAN",
                    CreditorAgent = "Receiver's BIC",
                    CurrencyCode = "EUR",
                    EndToEndId = "something",
                    RemittanceInfo = "Unstructured information for receiver",
                }
            }
        }
    }
};

var cctResp = c.CCT(cctParams);
```

# SSL verification

The verification process is done by using the default [**WebRequest**](https://msdn.microsoft.com/de-de/library/system.net.webrequest(v=vs.110).aspx) class.

# Limitations

* Usage with certificates has been prepared but not completely implemented yet. It works with private/public keys.
* Only version A005 for signatures can be used. A006 uses PSS padding, which is currently not supported by .NET Core 2.x. Bouncy Castle is only used for PEM file and certificate management.
* Only version E002 for encryption can be used.
* Only version X002 for authentication can be used.
* It was developed using EBICS Version H004, but H005 should work.

# Copyright & License

Copyright (c) 2016 - 2024 **Torsten Klinger**

Licensed under **GNU LESSER GENERAL PUBLIC LICENSE Version 3, 29 June 2007**. Please read the LICENSE file.

# Support

You can contact me via [E-Mail](mailto:torsten.klinger@googlemail.com).
