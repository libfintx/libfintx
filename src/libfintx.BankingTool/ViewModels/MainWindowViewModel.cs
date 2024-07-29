using System.Collections.Generic;
using System.Collections.ObjectModel;
using libfintx.BankingTool.Models;

namespace libfintx.BankingTool.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
#pragma warning disable CA1822 // Mark members as static
        public string Greeting => "Welcome to Avalonia!";
#pragma warning restore CA1822 // Mark members as static

        public ObservableCollection<BankAccount> BankAccounts { get; } = new ObservableCollection<BankAccount>();

    }
}
