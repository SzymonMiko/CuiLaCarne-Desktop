using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.ViewModels;

public partial class AuthenticationViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly SyncService _syncService;
    private readonly IRealtimeUpdateService _realtimeUpdateService;
    private readonly ISessionExpirationService _sessionExpirationService;
    private readonly INavigationService _navigation;
    private readonly IAppDialogService _dialog;

    [ObservableProperty]
    private string twoFactorCode = "";

    [ObservableProperty]
    private string statusMessage = "Wpisz 6-cyfrowy kod z aplikacji uwierzytelniającej.";

    public AuthenticationViewModel(
        AuthService authService,
        SyncService syncService,
        IRealtimeUpdateService realtimeUpdateService,
        ISessionExpirationService sessionExpirationService,
        INavigationService navigation,
        IAppDialogService dialog)
    {
        _authService = authService;
        _syncService = syncService;
        _realtimeUpdateService = realtimeUpdateService;
        _sessionExpirationService = sessionExpirationService;
        _navigation = navigation;
        _dialog = dialog;
    }

    [RelayCommand]
    private async Task Verify2FaAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SessionService.PendingTwoFactorToken))
            {
                StatusMessage = "Brak oczekującego logowania 2FA. Zaloguj się ponownie.";
                return;
            }

            var code = TwoFactorCode.Trim();

            if (code.Length != 6 || !code.All(char.IsDigit))
            {
                StatusMessage = "Wpisz 6 cyfr z aplikacji uwierzytelniającej.";
                return;
            }

            StatusMessage = "Weryfikowanie kodu...";

            var loginData = await _authService.Verify2FaAsync(
                SessionService.PendingTwoFactorToken,
                code);

            if (loginData == null || string.IsNullOrWhiteSpace(loginData.Token))
            {
                StatusMessage = "Weryfikacja 2FA nie powiodła się.";
                return;
            }

            var isAdmin = loginData.Roles.Any(role =>
                role.Equals("ROLE_MANAGER", StringComparison.OrdinalIgnoreCase)
                || role.Equals("ROLE_ADMIN", StringComparison.OrdinalIgnoreCase)
                || role.Equals("MANAGER", StringComparison.OrdinalIgnoreCase)
                || role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)
            );

            if (!isAdmin)
            {
                StatusMessage = "Brak dostępu. Aplikacja desktopowa jest tylko dla administratorów.";
                return;
            }

            SessionService.JwtToken = loginData.Token;
            SessionService.RefreshToken = loginData.RefreshToken;
            SessionService.Username = string.IsNullOrWhiteSpace(loginData.Username)
                ? SessionService.PendingTwoFactorUsername
                : loginData.Username;
            SessionService.PendingTwoFactorToken = "";
            SessionService.PendingTwoFactorUsername = "";
            SessionService.IsAdmin = true;

            _sessionExpirationService.StartWatching(loginData.Token);

            StatusMessage = "Synchronizowanie bazy danych...";
            await _syncService.SyncWholeDatabaseAsync(loginData.Token);
            await _realtimeUpdateService.StartAsync(loginData.Token);

            _dialog.ShowMessage("2FA zweryfikowane. Synchronizacja zakończona.");

            _navigation.ShowMenu();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }
}
