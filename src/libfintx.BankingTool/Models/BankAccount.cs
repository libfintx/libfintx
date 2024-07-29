using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using libfintx.BankingTool.BusinessLayer;
using libfintx.FinTS;
using libfintx.FinTS.Data;

namespace libfintx.BankingTool.Models;

public partial class BankAccount : ObservableObject
{
    [ObservableProperty]
    private string _accountHolder;

    [ObservableProperty]
    private string _iban;

    [ObservableProperty]
    private BankAccountSecrets _secrets;

    public void CreateConnectionDetails()
    {
        var iban = new Iban(Iban);

        var connectionDetails = new ConnectionDetails
        {
            AccountHolder = AccountHolder,
            Account = iban.AccountNumber,
            //Bic = iban.....,
            Blz = int.Parse(iban.BankIdent),
            Iban = Iban,
            Pin = Secrets.Pin,
        }
    }
}
