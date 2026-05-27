using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using QuiLaCarne.Models;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Services.Api;

public sealed class RestaurantWebSocketService : IRestaurantWebSocketService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] Topics =
    [
        "/topic/menu/dishes",
        "/topic/dictionary/dish-categories",
        "/topic/orders/updates",
        "/topic/orders/items",
        "/topic/dictionary/order-statuses",
        "/topic/dictionary/order-item-statuses",
        "/topic/reservations/updates",
        "/topic/tables/updates",
        "/topic/dictionary/table-statuses",
        "/topic/personnel/updates",
        "/topic/security/bans",
        "/topic/reports/updates",
        "/topic/dictionary/allergens",
        "/topic/dictionary/sync",
        "/topic/menu/availability",
    ];

    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _connectionCts;
    private Task? _listenTask;
    private string? _jwt;

    private static readonly Uri WebSocketUri =
        new("wss://api.quilacarne.com.pl/ws-qlc/websocket");

    public event EventHandler<WebSocketEvent>? EventReceived;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public async Task ConnectAsync(string jwt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            throw new ArgumentException("JWT token is required.", nameof(jwt));
        }

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            await DisconnectCoreAsync(cancellationToken);

            _jwt = jwt;
            _connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await OpenConnectionAsync(jwt, _connectionCts.Token);

            _listenTask = Task.Run(
                () => ListenWithReconnectAsync(_connectionCts.Token),
                CancellationToken.None);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            await DisconnectCoreAsync(cancellationToken);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _connectionLock.Dispose();
    }

    private async Task ListenWithReconnectAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!IsConnected && !string.IsNullOrWhiteSpace(_jwt))
                {
                    await OpenConnectionAsync(_jwt, cancellationToken);
                }

                var frame = await ReceiveFrameAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(frame) || frame == "\n")
                {
                    continue;
                }

                if (frame.StartsWith("MESSAGE", StringComparison.Ordinal))
                {
                    HandleMessageFrame(frame);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                await CloseSocketAsync(CancellationToken.None);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private async Task OpenConnectionAsync(string jwt, CancellationToken cancellationToken)
    {
        await CloseSocketAsync(CancellationToken.None);

        _webSocket = new ClientWebSocket();
        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {jwt}");

        await _webSocket.ConnectAsync(WebSocketUri, cancellationToken);

        await SendFrameAsync(
            "CONNECT",
            new Dictionary<string, string>
            {
                ["accept-version"] = "1.1,1.2",
                ["heart-beat"] = "10000,10000",
                ["Authorization"] = $"Bearer {jwt}",
            },
            cancellationToken);

        var connectedFrame = await ReceiveFrameAsync(cancellationToken);

        if (!connectedFrame.StartsWith("CONNECTED", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("STOMP connection was not accepted by the server.");
        }

        for (var i = 0; i < Topics.Length; i++)
        {
            await SendFrameAsync(
                "SUBSCRIBE",
                new Dictionary<string, string>
                {
                    ["id"] = $"sub-{i}",
                    ["destination"] = Topics[i],
                },
                cancellationToken);
        }
    }

    private void HandleMessageFrame(string frame)
    {
        var separator = "\n\n";
        var separatorIndex = frame.IndexOf(separator, StringComparison.Ordinal);

        if (separatorIndex < 0)
        {
            separator = "\r\n\r\n";
            separatorIndex = frame.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        }

        if (separatorIndex < 0)
        {
            return;
        }

        var body = frame[(separatorIndex + separator.Length)..].TrimEnd('\0', '\r', '\n');
        var websocketEvent = JsonSerializer.Deserialize<WebSocketEvent>(body, JsonOptions);

        if (websocketEvent is not null)
        {
            EventReceived?.Invoke(this, websocketEvent);
        }
    }

    private async Task SendFrameAsync(
        string command,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        if (_webSocket is null)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine(command);

        foreach (var (key, value) in headers)
        {
            builder.AppendLine($"{key}:{value}");
        }

        builder.Append("\n\0");

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());

        await _webSocket.SendAsync(
            bytes,
            WebSocketMessageType.Text,
            true,
            cancellationToken);
    }

    private async Task<string> ReceiveFrameAsync(CancellationToken cancellationToken)
    {
        if (_webSocket is null)
        {
            return "";
        }

        var buffer = new byte[8192];
        using var stream = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await _webSocket.ReceiveAsync(buffer, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return "";
            }

            stream.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private async Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        _connectionCts?.Cancel();

        if (_listenTask is not null)
        {
            try
            {
                await _listenTask.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch
            {
                // The socket can be blocked in a receive call. Closing below releases it.
            }
        }

        await CloseSocketAsync(cancellationToken);

        _connectionCts?.Dispose();
        _connectionCts = null;
        _listenTask = null;
    }

    private async Task CloseSocketAsync(CancellationToken cancellationToken)
    {
        var socket = _webSocket;
        _webSocket = null;

        if (socket is null)
        {
            return;
        }

        try
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client disconnecting.",
                    cancellationToken);
            }
        }
        catch
        {
            // A half-open socket is safe to dispose.
        }
        finally
        {
            socket.Dispose();
        }
    }
}
