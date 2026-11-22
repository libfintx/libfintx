# EBICS

`libfintx.EBICS` is a client for the Electronic Banking Internet Communication Standard (EBICS). It is available on NuGet as [libfintx.EBICS](https://www.nuget.org/packages/libfintx.EBICS/) and runs on .NET 6, .NET 8 and .NET 10.

The EBICS specification and its appendices are in this folder.

## Features

* EBICS version **H004**; **H005** can be selected
* Initialization: send the public keys (**INI**, **HIA**) and get the bank's public keys (**HPB**), and create the INI and HIA letters
* Send a credit transfer initiation (**CCT**)
* Send a direct debit initiation (**CDD**)
* Pick up SWIFT daily statements (**STA**)
* Pick up camt.052 / camt.053 reports (**C52**, **C53**)
* Get the customer protocol (**PTK**)
* Suspend access (**SPR**)
* **E002** for encryption
* **X002** for authentication
* **A005** and **A006** for signatures

## Limitations

* Certificates have been prepared but are not completely implemented. It works with private/public keys.
* Only E002 can be used for encryption and only X002 for authentication.
* It was developed against EBICS H004; H005 should work.

## Sample code


### Initialization (INI/HIA)

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

### Retrieving public bank keys (HPB)

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

### Direct credit transfer (CCT)

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

