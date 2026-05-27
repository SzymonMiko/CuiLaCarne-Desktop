using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.ViewModels
{
    public partial class AuthenticationViewModel : ObservableObject
    {
        [ObservableProperty]
        private string twoFactorCode = "";

        [ObservableProperty]
        private string statusMessage = "";

        [RelayCommand]
        private async Task Verify2FaAsync()
        {
            StatusMessage = "2FA verification not connected yet.";
            await Task.CompletedTask;
        }
    }
}
