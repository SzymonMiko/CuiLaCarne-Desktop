using QuiLaCarne.Models;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Tests.ViewModels;

internal sealed class FakeRealtimeUpdateService : IRealtimeUpdateService
{
    public event EventHandler<WebSocketEvent>? LocalDataChanged;

    public List<string> StartedTokens { get; } = [];

    public int StopCount { get; private set; }

    public Task StartAsync(string jwt, CancellationToken cancellationToken = default)
    {
        StartedTokens.Add(jwt);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        StopCount++;
        return Task.CompletedTask;
    }

    public void Publish(WebSocketEvent websocketEvent)
    {
        LocalDataChanged?.Invoke(this, websocketEvent);
    }
}
