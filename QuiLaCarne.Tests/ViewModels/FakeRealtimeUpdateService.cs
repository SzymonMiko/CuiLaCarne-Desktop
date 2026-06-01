using QuiLaCarne.Models;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Tests.ViewModels;

internal sealed class FakeRealtimeUpdateService : IRealtimeUpdateService
{
    public event EventHandler<WebSocketEvent>? LocalDataChanged;

    public Task StartAsync(string jwt, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Publish(WebSocketEvent websocketEvent)
    {
        LocalDataChanged?.Invoke(this, websocketEvent);
    }
}
