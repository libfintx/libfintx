using CommunityToolkit.Mvvm.ComponentModel;

namespace libfintx.BankingTool.Models;

public partial class BankAccountSecrets : ObservableObject
{
    [ObservableProperty]
    private string _pin;
}
