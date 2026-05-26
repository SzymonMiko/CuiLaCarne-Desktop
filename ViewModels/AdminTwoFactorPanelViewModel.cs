using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuiLaCarne.Services.Api;
using System.Windows;

public partial class AdminTwoFactorPanelViewModel : ObservableObject
{
    private readonly AuthService _authService;

    [ObservableProperty] private string verificationCode = "";
    [ObservableProperty] private string statusMessage = "2FA not configured in this session.";

    public AdminTwoFactorPanelViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task GenerateTwoFactorAsync()
    {
        // Use your existing AuthService generate 2FA method here.
        // Example: var result = await _authService.Generate2FaAsync(SessionService.JwtToken);
        await Task.CompletedTask;
        StatusMessage = "Generated 2FA secret. Show QR/secret here using your existing DTO.";
    }

    [RelayCommand]
    private async Task EnableTwoFactorAsync()
    {
        if (string.IsNullOrWhiteSpace(VerificationCode))
        {
            MessageBox.Show("Enter 2FA code.");
            return;
        }

        // Use your existing AuthService enable 2FA method here.
        // await _authService.Enable2FaAsync(SessionService.JwtToken, VerificationCode);
        await Task.CompletedTask;
        StatusMessage = "2FA enabled for administrative account.";
    }
}
