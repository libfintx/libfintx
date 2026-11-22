# FinTS / HBCI

`libfintx.FinTS` is a client for **HBCI 2.2** and **FinTS 3.0** with **PIN/TAN**. It is available on NuGet as [libfintx.FinTS](https://www.nuget.org/packages/libfintx.FinTS/) and runs on .NET Standard 2.0, .NET 6, .NET 8 and .NET 10.

For the specification see the [FinTS documents of the Deutsche Kreditwirtschaft](https://www.hbci-zka.de/spec/spezifikation.htm); copies are in this folder.

## Features

### Accounts and statements

* Synchronization and end of a dialog (**HKSYN**, **HKEND**)
* Get the accounts from the user parameter data (UPD), with the FinTS account kind
* Get the balance (**HKSAL**)
* Request transactions as MT940 (**HKKAZ**)
* Request transactions as camt.052 / camt.053 (**HKCAZ**)
* Get the securities holdings (Depotaufstellung) as MT535 (**HKWPD**)

### Payments

* Transfer money (**HKCCS**)
* Transfer money at a certain time (**HKCSE**)
* Get, modify and delete scheduled transfers (**HKCSB**, **HKCSA**, **HKCSL**)
* Collective transfer (**HKCCM**) and collective transfer at a certain time (**HKCME**)
* Rebook money from one account to another (**HKCUM**)
* Collect money (**HKDSE**) and collective collect (**HKDME**)
* Submit, get, modify and delete banker's orders (**HKCDE**, **HKCDB**, **HKCDN**, **HKCDL**)
* Load a mobile phone prepaid card (**HKPPD**)

### Strong customer authentication

* TAN process 2 (**HKTAN**)
* Decoupled approval in the banking app (**HKTAN#7**), using the status request parameters from HITANS#7; `HBCIDialogResult.IsAuthenticationPending` tells whether an approval is outstanding
* chipTAN flicker code and photoTAN
* Get the names of the TAN media (**HKTAB**)

### Parameter data

* The bank parameter data (BPD) and user parameter data (UPD) are cached; choose where they are stored with `IBpdStore` and `IUpdStore` (file store by default, in-memory store available)

### File formats

* SWIFT **MT940** account statements and **MT535** securities statements (`libfintx.Swift`)
* **camt.052** and **camt.053** reports
* SEPA credit transfers (**pain.001.001.03**, **pain.001.002.03**, **pain.001.003.03**) and direct debits (**pain.008.002.02**) (`libfintx.Sepa`)

## Sample code


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

        // Every business transaction opens a dialog; end it once nothing is outstanding
        await client.EndDialog();
    }
}
```


More complete examples are in [libfintx.Sample](../src/libfintx.Sample) and [libfintx.Sample.Ui](../src/libfintx.Sample.Ui).
