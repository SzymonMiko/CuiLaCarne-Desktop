using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;
using System.Windows;
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
    private readonly RestaurantWebSocketService _webSocketService;

    [ObservableProperty]
    private string username = "";

    [ObservableProperty]
    private string password = "";

    public LoginViewModel(

        INavigationService navigation,
        RestaurantWebSocketService webSocketService,
            ReservationService reservationService,
        DishService dishService,
        SystemService systemService,
        AuthService authService,
        SyncService syncService,
        LookupService lookupService)
    {
        _webSocketService = webSocketService;   
        _reservationService = reservationService;
        _dishService = dishService;
        _systemService = systemService;
        _authService = authService;
        _syncService = syncService;
        _lookupService = lookupService;
        _navigation = navigation;
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        try
        {
            var loginData =
                await _authService.LoginAsync(
                    Username,
                    Password);

            if (loginData == null ||
                string.IsNullOrWhiteSpace(loginData.Token))
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


            MessageBox.Show("Login success");

            await _webSocketService.ConnectAsync(token);

            var cacheKeys =
                await _systemService.GetCacheListAsync(token);

            MessageBox.Show(string.Join("\n", cacheKeys));

            var menu =
                await _dishService.GetFullRestaurantMenuAsync(token);

            MessageBox.Show($"Menu count: {menu.Count}");

            var allergens =
                await _lookupService.GetAllergensAsync(token, "pl");

            MessageBox.Show($"Allergens: {allergens.Count}");

            var banStatuses =
                await _lookupService.GetBanStatusesAsync(token, "pl");

            MessageBox.Show($"Ban statuses: {banStatuses.Count}");

            var categories =
                await _lookupService.GetDishCategoriesAsync(token, "pl");

            MessageBox.Show($"Dish categories: {categories.Count}");

            var ingredients =
                await _lookupService.GetIngredientsAsync(token, "pl");

            MessageBox.Show($"Ingredients: {ingredients.Count}");

            var orderStatuses =
                await _lookupService.GetOrderStatusesAsync(token);

            MessageBox.Show($"Order statuses: {orderStatuses.Count}");

            var orderItemStatuses =
                await _lookupService.GetOrderItemStatusesAsync(token);

            MessageBox.Show($"Order item statuses: {orderItemStatuses.Count}");

            var reservationStatuses =
                await _lookupService.GetReservationStatusesAsync(token);

            MessageBox.Show($"Reservation statuses: {reservationStatuses.Count}");

            var manifest =
                await _syncService.DownloadBootstrapManifestAsync(token);

            MessageBox.Show(
                $"Server time: {manifest?.ServerTime}\nModules: {manifest?.Modules.Count}");

            await _syncService.SyncUsersAsync(token);
            await _syncService.SyncTablesAsync(token);
            await _syncService.SyncIngredientsAsync(token);
            await _syncService.SyncBansAsync(token);
            await _syncService.SyncDishesAsync(token);

            MessageBox.Show("Sync finished");

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
