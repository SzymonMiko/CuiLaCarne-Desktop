using Microsoft.Extensions.DependencyInjection;
using QuiLaCarne.Models;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Services.Api;

public sealed class RealtimeUpdateService : IRealtimeUpdateService
{
    private readonly IRestaurantWebSocketService _webSocketService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private string? _jwt;

    public RealtimeUpdateService(
        IRestaurantWebSocketService webSocketService,
        IServiceScopeFactory scopeFactory)
    {
        _webSocketService = webSocketService;
        _scopeFactory = scopeFactory;
        _webSocketService.EventReceived += OnEventReceived;
    }

    public event EventHandler<WebSocketEvent>? LocalDataChanged;

    public async Task StartAsync(string jwt, CancellationToken cancellationToken = default)
    {
        _jwt = jwt;
        await _webSocketService.ConnectAsync(jwt, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _jwt = null;
        return _webSocketService.DisconnectAsync(cancellationToken);
    }

    private void OnEventReceived(object? sender, WebSocketEvent websocketEvent)
    {
        _ = SyncAndPublishAsync(websocketEvent);
    }

    private async Task SyncAndPublishAsync(WebSocketEvent websocketEvent)
    {
        if (string.IsNullOrWhiteSpace(_jwt))
        {
            return;
        }

        await _syncLock.WaitAsync();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();
            var lookupService = scope.ServiceProvider.GetRequiredService<LookupService>();

            await SyncForEventAsync(syncService, lookupService, websocketEvent, _jwt);

            LocalDataChanged?.Invoke(this, websocketEvent);
        }
        catch
        {
           
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private static async Task SyncForEventAsync(
        SyncService syncService,
        LookupService lookupService,
        WebSocketEvent websocketEvent,
        string jwt)
    {
        switch (websocketEvent.EntityType.ToUpperInvariant())
        {
            case "DISH":
            case "DISH_AVAILABILITY":
            case "MENU_AVAILABILITY":
                await lookupService.GetDishCategoriesAsync(jwt);
                await syncService.SyncIngredientsAsync(jwt);
                await syncService.SyncDishesAsync(jwt);
                break;

            case "CATEGORY":
                await lookupService.GetDishCategoriesAsync(jwt);
                await syncService.SyncDishesAsync(jwt);
                break;

            case "INGREDIENT":
                await syncService.SyncIngredientsAsync(jwt);
                await syncService.SyncDishesAsync(jwt);
                break;

            case "ORDER":
                await syncService.SyncOrdersAsync(jwt);
                await syncService.SyncOrderItemsAsync(jwt);
                break;

            case "ORDER_ITEM":
                await syncService.SyncOrderItemsAsync(jwt);
                break;

            case "ORDER_STATUS":
                await lookupService.GetOrderStatusesAsync(jwt);
                await syncService.SyncOrdersAsync(jwt);
                break;

            case "ORDER_ITEM_STATUS":
                await lookupService.GetOrderItemStatusesAsync(jwt);
                await syncService.SyncOrderItemsAsync(jwt);
                break;

            case "RESERVATION":
                await syncService.SyncReservationsAsync(jwt);
                break;

            case "TABLE":
                await lookupService.GetTableStatusesAsync(jwt);
                await syncService.SyncTablesAsync(jwt);
                break;

            case "TABLE_STATUS":
                await lookupService.GetTableStatusesAsync(jwt);
                await syncService.SyncTablesAsync(jwt);
                break;

            case "EMPLOYEE":
                await syncService.SyncUsersAsync(jwt);
                break;

            case "BAN":
                await syncService.SyncUsersAsync(jwt);
                await syncService.SyncBansAsync(jwt);
                break;
        }
    }
}
