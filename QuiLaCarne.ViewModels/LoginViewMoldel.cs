using System.Windows;
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
        ISessionExpirationService sessionExpirationService
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
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        try
        {
            var loginData = await _authService.LoginAsync(Username, Password);

            if (loginData == null || string.IsNullOrWhiteSpace(loginData.Token))
            {
                MessageBox.Show("Login failed");
                return;
            }

            if (loginData.Requires2fa)
            {
                MessageBox.Show("2FA required");
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
                MessageBox.Show("Access denied. Desktop app is only for administrators.");
                return;
            }

            SessionService.JwtToken = token;
            SessionService.RefreshToken = loginData.RefreshToken;
            SessionService.Username = loginData.Username;
            SessionService.IsAdmin = true;
            _sessionExpirationService.StartWatching(token);

            await _syncService.SyncWholeDatabaseAsync(token);

            MessageBox.Show("Sync finished");

            await _realtimeUpdateService.StartAsync(token);

            _navigation.ShowMenu();
            _navigation.CloseLogin();
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show($"HTTP error:\n{ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"Authorization error:\n{ex.Message}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error:\n{ex}");
        }
    }
}
