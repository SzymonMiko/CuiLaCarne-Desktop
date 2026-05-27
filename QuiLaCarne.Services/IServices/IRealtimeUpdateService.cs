using QuiLaCarne.Models;

namespace QuiLaCarne.Services.IServices;

public interface IRealtimeUpdateService
{
    event EventHandler<WebSocketEvent>? LocalDataChanged;

    Task StartAsync(string jwt, CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
