using libfintx.EBICS.Parameters;
using libfintx.EBICS;
using libfintx.EBICSConfig;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using libfintx.Security;
using Serilog;


bool ini = true;
IEbicsClient client = null;
string path = AppDomain.CurrentDomain.BaseDirectory;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .CreateLogger();

EbicsLogging.MethodLoggingEnabled = true;
EbicsLogging.LoggerFactory.AddSerilog();

string adresse = "https://isotest.postfinance.ch/ebicsweb/ebicsweb";
string hostid = "PFEBICS";
string partnerid = "xxx";
string userid = "xxx";
string pwd = "Buchen123";
string dn = "torsten.klinger@googlemail.com";
string bn = "Postfinanz";
string usr = "torstenklinger";


#region Keys erstellen
if (!File.Exists(Path.Combine(path, "sign.key")))
{
    KeyUtils.GenerateDE(path, dn, pwd, hostid, bn, userid, usr, partnerid);
}
#endregion Keys erstellen

#region Keys lesen
IPasswordFinder pwf = new PwUtils(pwd);

AsymmetricCipherKeyPair signKey;
using (var sr = new StringReader(File.ReadAllText(Path.Combine(path, "sign.key")).Trim()))
{
    var pr = new PemReader(sr, pwf);
    signKey = (AsymmetricCipherKeyPair) pr.ReadObject();
}

AsymmetricCipherKeyPair authKey;
using (var sr = new StringReader(File.ReadAllText(Path.Combine(path, "auth.key")).Trim()))
{
    var pr = new PemReader(sr, pwf);
    authKey = (AsymmetricCipherKeyPair) pr.ReadObject();
}


AsymmetricCipherKeyPair encKey;
using (var sr = new StringReader(File.ReadAllText(Path.Combine(path, "enc.key")).Trim()))
{
    var pr = new PemReader(sr, pwf);
    encKey = (AsymmetricCipherKeyPair) pr.ReadObject();
}
#endregion Keys lesen

if (ini == true)
{
    #region INI
    client = EbicsClient.Factory().Create(new Config
    {
        Address = adresse, // "The EBICS URL you got from your bank, i.e. https://ebics-server.com/",
        Insecure = true,
        TLS = true,
        User = new UserParams
        {
            HostId = hostid,
            PartnerId = partnerid,
            UserId = userid,
            SignKeys = new SignKeyPair
            {
                Version = SignVersion.A006,
                TimeStamp = DateTime.Now,
                Certificate = KeyUtils.ReadCert(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sign.p12"), pwd)
            }
        }
    });

    var iniResp = client.INI(new IniParams());
    #endregion INI

    #region HIA
    client = EbicsClient.Factory().Create(new Config
    {
        Address = adresse, // "The EBICS URL you got from your bank, i.e. https://ebics-server.com/",
        Insecure = true,
        TLS = true,
        User = new UserParams
        {
            HostId = hostid,
            PartnerId = partnerid,
            UserId = userid,
            AuthKeys = new AuthKeyPair
            {
                Version = AuthVersion.X002,
                TimeStamp = DateTime.Now,
                Certificate = KeyUtils.ReadCert(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auth.p12"), pwd)
            },
            CryptKeys = new CryptKeyPair
            {
                Version = CryptVersion.E002,
                TimeStamp = DateTime.Now,
                Certificate = KeyUtils.ReadCert(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "enc.p12"), pwd)
            }
        }
    });

    var hiaResp = client.HIA(new HiaParams());
    #endregion HIA

}
else
{
    #region HBP
    client = EbicsClient.Factory().Create(new Config
    {
        Address = adresse, // "The EBICS URL you got from your bank, i.e. https://ebics-server.com/",
        Insecure = true,
        TLS = true,
        User = new UserParams
        {
            HostId = hostid,
            PartnerId = partnerid,
            UserId = userid,
            AuthKeys = new AuthKeyPair
            {
                Version = AuthVersion.X002,
                TimeStamp = DateTime.Now,
                Certificate = KeyUtils.ReadCert(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auth.p12"), pwd)
            },
            CryptKeys = new CryptKeyPair
            {
                Version = CryptVersion.E002,
                TimeStamp = DateTime.Now,
                Certificate = KeyUtils.ReadCert(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "enc.p12"), pwd)
            },
            SignKeys = new SignKeyPair
            {
                Version = SignVersion.A006,
                TimeStamp = DateTime.Now,
                Certificate = KeyUtils.ReadCert(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sign.p12"), pwd)
            }
        }
    });

    var hpbResp = client.HPB(new HpbParams());
    if (hpbResp.TechnicalReturnCode != 0 || hpbResp.BusinessReturnCode != 0)
    {
        return;
    }

    client.Config.Bank = hpbResp.Bank;
    #endregion HBP

    #region CCT
    var cctParams = new CctParams
    {
        InitiatingParty = "Torsten Klinger",
        PaymentInfos = new[]
        {
            new CreditTransferPaymentInfo
            {
                DebtorName = "Torsten Klinger",
                DebtorAccount = "xxx",
                DebtorAgent = "POFICHBEXXX",
                ExecutionDate = "2024-04-11",
                CreditTransferTransactionInfos = new[]
                {
                    new CreditTransferTransactionInfo
                    {
                        Amount = "1.00",
                        CreditorName = "Torsten Klinger",
                        CreditorAccount = "xxx",
                        CreditorAgent = "POFICHBEXXX",
                        CurrencyCode = "EUR",
                        EndToEndId = "Buchen",
                        RemittanceInfo = "Buchen",
                    }
                }
            }
        }
    };

    try
    {
        var cctResp = client.CCT(cctParams);
        if (cctResp.TechnicalReturnCode != 0 || hpbResp.BusinessReturnCode != 0)
        {
            return;
        }
    }
    catch (Exception ex)
    {
        Log.Logger.Error(ex.Message);
    }
    #endregion CCT

    #region STA
    var staParams = new StaParams
    {
        StartDate = DateTime.Now,
        EndDate = DateTime.Now
    };

    try
    {
        var staResp = client.STA(staParams);
        if (staResp.TechnicalReturnCode != 0 || hpbResp.BusinessReturnCode != 0)
        {
            return;
        }
    }
    catch (Exception ex)
    {
        Log.Logger.Error(ex.Message);
    }

    #endregion STA
}

