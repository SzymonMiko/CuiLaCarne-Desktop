using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
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
            var db = scope.ServiceProvider.GetRequiredService<QuiLaCarneDbContext>();

            await ApplyDeleteEventAsync(db, websocketEvent);
            await ApplyDishAvailabilityEventAsync(db, websocketEvent);
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

    private static async Task ApplyDishAvailabilityEventAsync(
        QuiLaCarneDbContext db,
        WebSocketEvent websocketEvent)
    {
        var entityType = websocketEvent.EntityType.ToUpperInvariant();

        if (entityType != "DISH_AVAILABILITY" && entityType != "MENU_AVAILABILITY")
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(websocketEvent.Token))
        {
            return;
        }

        var dish = await db.Dishes.FirstOrDefaultAsync(x => x.Token == websocketEvent.Token);

        if (dish == null)
        {
            return;
        }

        var available = TryReadPayloadBool(websocketEvent, "available")
            ?? TryReadPayloadBool(websocketEvent, "isAvailable");
        var availableFrom = TryReadPayloadDateTimeOffset(websocketEvent, "availableFrom");

        if (available == true)
        {
            dish.AvailableFrom = null;
        }
        else if (available == null && PayloadHasNullProperty(websocketEvent, "availableFrom"))
        {
            dish.AvailableFrom = null;
        }
        else
        {
            dish.AvailableFrom = availableFrom ?? DateTimeOffset.MaxValue;
        }

        await db.SaveChangesAsync();
    }

    private static bool? TryReadPayloadBool(WebSocketEvent websocketEvent, string propertyName)
    {
        if (websocketEvent.Payload is not { ValueKind: System.Text.Json.JsonValueKind.Object } payload ||
            !payload.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is not System.Text.Json.JsonValueKind.True and not System.Text.Json.JsonValueKind.False)
        {
            return null;
        }

        return property.GetBoolean();
    }

    private static DateTimeOffset? TryReadPayloadDateTimeOffset(
        WebSocketEvent websocketEvent,
        string propertyName)
    {
        if (websocketEvent.Payload is not { ValueKind: System.Text.Json.JsonValueKind.Object } payload ||
            !payload.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            return null;
        }

        return DateTimeOffset.TryParse(property.GetString(), out var value) ? value : null;
    }

    private static bool PayloadHasNullProperty(WebSocketEvent websocketEvent, string propertyName)
    {
        return websocketEvent.Payload is { ValueKind: System.Text.Json.JsonValueKind.Object } payload &&
            payload.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == System.Text.Json.JsonValueKind.Null;
    }

    private static async Task ApplyDeleteEventAsync(
        QuiLaCarneDbContext db,
        WebSocketEvent websocketEvent)
    {
        if (!websocketEvent.EventType.Equals("DELETED", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(websocketEvent.Token))
        {
            return;
        }

        var entityType = websocketEvent.EntityType.ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(entityType) && entityType != "INGREDIENT")
        {
            return;
        }

        var ingredient = await db.Ingredients
            .FirstOrDefaultAsync(x => x.Token == websocketEvent.Token);

        if (ingredient == null)
        {
            return;
        }

        db.Ingredients.Remove(ingredient);
        await db.SaveChangesAsync();

        if (string.IsNullOrWhiteSpace(websocketEvent.EntityType))
        {
            websocketEvent.EntityType = "INGREDIENT";
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
                await lookupService.GetDishCategoriesAsync(jwt);
                await syncService.SyncIngredientsAsync(jwt);
                await syncService.SyncDishesAsync(jwt);
                break;

            case "DISH_AVAILABILITY":
            case "MENU_AVAILABILITY":
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
