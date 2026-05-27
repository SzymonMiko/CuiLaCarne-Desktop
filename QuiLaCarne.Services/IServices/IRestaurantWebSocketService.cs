using QuiLaCarne.Models;

namespace QuiLaCarne.Services.IServices;

public interface IRestaurantWebSocketService : IAsyncDisposable
{
    event EventHandler<WebSocketEvent>? EventReceived;

    bool IsConnected { get; }

    Task ConnectAsync(string jwt, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
