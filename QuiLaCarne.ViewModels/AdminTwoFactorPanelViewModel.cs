using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;

public partial class AdminTwoFactorPanelViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly IAppDialogService _dialog;

    [ObservableProperty] private string verificationCode = "";
    [ObservableProperty] private string statusMessage = "2FA nie jest skonfigurowane w tej sesji.";
    [ObservableProperty] private string qrCodeImageUrl = "";
    [ObservableProperty] private string manualEntryKey = "";

    public AdminTwoFactorPanelViewModel(AuthService authService, IAppDialogService dialog)
    {
        _authService = authService;
        _dialog = dialog;
    }

    [RelayCommand]
    private async Task GenerateTwoFactorAsync()
    {
        try
        {
            var result = await _authService.Generate2FaQrCodeAsync(SessionService.JwtToken);

            if (result == null)
            {
                StatusMessage = "Nie udało się wygenerować danych konfiguracji 2FA.";
                return;
            }

            QrCodeImageUrl = result.QrCodeImageUrl;
            ManualEntryKey = result.ManualEntryKey;
            StatusMessage = "Zeskanuj kod QR albo wpisz klucz ręcznie, a potem podaj kod z aplikacji.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task EnableTwoFactorAsync()
    {
        if (string.IsNullOrWhiteSpace(VerificationCode))
        {
            _dialog.ShowMessage("Wpisz kod 2FA.");
            return;
        }

        try
        {
            var enabled = await _authService.Enable2FaAsync(
                SessionService.JwtToken,
                VerificationCode.Trim());

            StatusMessage = enabled
                ? "2FA zostało włączone dla tego konta."
                : "2FA nie zostało włączone.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }
}
