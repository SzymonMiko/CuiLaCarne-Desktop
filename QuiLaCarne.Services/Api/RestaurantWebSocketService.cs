using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuiLaCarne.Models;
using System.Net.WebSockets;
using System.Text.Json;

namespace QuiLaCarne.Services.Api;

public class RestaurantWebSocketService
{
    private ClientWebSocket? _ws;
    private readonly SyncService _syncService;

    public event Action<WebSocketEvent>? EventReceived;

    private const string WebSocketUrl =
        "wss://api.quilacarne.com.pl/ws-qlc/websocket";

    private readonly string[] _topics =
    {
        "/topic/orders/updates",
        "/topic/orders/items",
        "/topic/menu/dishes",
        "/topic/dictionary/dish-categories",
        "/topic/reservations/updates",
        "/topic/tables/updates",
        "/topic/personnel/updates",
        "/topic/security/bans",
        "/topic/reports/updates",
        "/topic/dictionary/sync",
        "/topic/menu/availability"
    };

    public RestaurantWebSocketService(
        SyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task ConnectAsync(string jwt)
    {
        _ws = new ClientWebSocket();

        _ws.Options.SetRequestHeader(
            "Authorization",
            $"Bearer {jwt}");

        await _ws.ConnectAsync(
            new Uri(WebSocketUrl),
            CancellationToken.None);

        await SendFrameAsync(
            "CONNECT\n" +
            "accept-version:1.1,1.2\n" +
            $"Authorization:Bearer {jwt}\n" +
            "\n\0");

        await ReceiveFrameAsync();

        for (int i = 0; i < _topics.Length; i++)
        {
            await SendFrameAsync(
                "SUBSCRIBE\n" +
                $"id:sub-{i}\n" +
                $"destination:{_topics[i]}\n" +
                "\n\0");
        }

        _ = Task.Run(() => ListenAsync(jwt));
    }

    private async Task ListenAsync(string jwt)
    {
        while (_ws != null &&
               _ws.State == WebSocketState.Open)
        {
            var frame =
                await ReceiveFrameAsync();

            if (!frame.StartsWith("MESSAGE"))
                continue;

            var separatorIndex =
                frame.IndexOf("\n\n");

            if (separatorIndex == -1)
                continue;

            var json =
                frame[(separatorIndex + 2)..]
                    .TrimEnd('\0');

            await HandleMessageAsync(json, jwt);
        }
    }

    private async Task HandleMessageAsync(
        string json,
        string jwt)
    {
        var wsEvent =
            JsonSerializer.Deserialize<WebSocketEvent>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (wsEvent == null)
            return;

        switch (wsEvent.EntityType)
        {
            case "ORDER":
                await _syncService.SyncOrdersAsync(jwt);
                break;

            case "ORDER_ITEM":
                await _syncService.SyncOrderItemsAsync(jwt);
                break;

            case "DISH":
                await _syncService.SyncDishesAsync(jwt);
                break;

            case "CATEGORY":
                await _syncService.SyncDishCategoriesAsync(jwt);
                break;

            case "RESERVATION":
                await _syncService.SyncReservationsAsync(jwt);
                break;

            case "TABLE":
                await _syncService.SyncTablesAsync(jwt);
                break;

            case "EMPLOYEE":
                await _syncService.SyncUsersAsync(jwt);
                break;

            case "BAN":
                await _syncService.SyncBansAsync(jwt);
                break;

            case "INGREDIENT":
                await _syncService.SyncIngredientsAsync(jwt);
                await _syncService.SyncDishesAsync(jwt);
                break;
        }

        EventReceived?.Invoke(wsEvent);
    }

    private async Task SendFrameAsync(string frame)
    {
        if (_ws == null)
            return;

        var bytes =
            Encoding.UTF8.GetBytes(frame);

        await _ws.SendAsync(
            bytes,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    private async Task<string> ReceiveFrameAsync()
    {
        if (_ws == null)
            return "";

        var buffer =
            new byte[65536];

        var result =
            await _ws.ReceiveAsync(
                buffer,
                CancellationToken.None);

        return Encoding.UTF8.GetString(
            buffer,
            0,
            result.Count);
    }
}
