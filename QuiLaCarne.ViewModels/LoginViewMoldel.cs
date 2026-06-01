using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ReservationService _reservationService;
    private readonly AuthService _authService;
    private readonly SystemService _systemService;
    private readonly SyncService _syncService;
    private readonly LookupService _lookupService;
    private readonly DishService _dishService;
    private readonly INavigationService _navigation;
    private readonly IRealtimeUpdateService _realtimeUpdateService;
    private readonly ISessionExpirationService _sessionExpirationService;
    private readonly IAppDialogService _dialog;

    [ObservableProperty]
    private string username = "";

    [ObservableProperty]
    private string password = "";

    public LoginViewModel(
        INavigationService navigation,
        IRealtimeUpdateService realtimeUpdateService,
        ReservationService reservationService,
        DishService dishService,
        SystemService systemService,
        AuthService authService,
        SyncService syncService,
        LookupService lookupService,
        ISessionExpirationService sessionExpirationService,
        IAppDialogService dialog
    )
    {
        _realtimeUpdateService = realtimeUpdateService;
        _reservationService = reservationService;
        _dishService = dishService;
        _systemService = systemService;
        _authService = authService;
        _syncService = syncService;
        _lookupService = lookupService;
        _navigation = navigation;
        _sessionExpirationService = sessionExpirationService;
        _dialog = dialog;
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        try
        {
            var loginData = await _authService.LoginAsync(Username, Password);

            if (loginData == null || string.IsNullOrWhiteSpace(loginData.Token))
            {
                _dialog.ShowMessage("Logowanie nie powiodło się.");
                return;
            }

            if (loginData.Requires2fa)
            {
                if (string.IsNullOrWhiteSpace(loginData.Token))
                {
                    _dialog.ShowMessage("Wymagane 2FA, ale serwer nie zwrócił tokenu weryfikacyjnego.");
                    return;
                }

                SessionService.PendingTwoFactorToken = loginData.Token;
                SessionService.PendingTwoFactorUsername = string.IsNullOrWhiteSpace(loginData.Username)
                    ? Username
                    : loginData.Username;

                _navigation.ShowTwoFactor();
                _navigation.CloseLogin();
                return;
            }

            var token = loginData.Token;
            var isAdmin = loginData.Roles.Any(role =>
                role.Equals("ROLE_MANAGER", StringComparison.OrdinalIgnoreCase)
                || role.Equals("ROLE_ADMIN", StringComparison.OrdinalIgnoreCase)
                || role.Equals("MANAGER", StringComparison.OrdinalIgnoreCase)
                || role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)
            );
            if (!isAdmin)
            {
                _dialog.ShowMessage("Brak dostępu. Aplikacja desktopowa jest tylko dla administratorów.");
                return;
            }

            SessionService.JwtToken = token;
            SessionService.RefreshToken = loginData.RefreshToken;
            SessionService.Username = loginData.Username;
            SessionService.IsAdmin = true;
            _sessionExpirationService.StartWatching(token);

            await _syncService.SyncWholeDatabaseAsync(token);

            _dialog.ShowMessage("Synchronizacja zakończona.");

            await _realtimeUpdateService.StartAsync(token);

            _navigation.ShowMenu();
            _navigation.CloseLogin();
        }
        catch (HttpRequestException ex)
        {
            _dialog.ShowMessage($"Błąd HTTP:\n{ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _dialog.ShowMessage($"Błąd autoryzacji:\n{ex.Message}");
        }
        catch (Exception ex)
        {
            _dialog.ShowMessage($"Nieoczekiwany błąd:\n{ex}");
        }
    }
}
