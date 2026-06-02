using Microsoft.Extensions.DependencyInjection;
using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class RealtimeUpdateServiceTests
{
    [Fact]
    public async Task StartAndStop_DelegateToWebSocketService()
    {
        var webSocket = new FakeRestaurantWebSocketService();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var service = new RealtimeUpdateService(
            webSocket,
            serviceProvider.GetRequiredService<IServiceScopeFactory>());

        await service.StartAsync("jwt-token");
        await service.StopAsync();

        Assert.Equal("jwt-token", Assert.Single(webSocket.ConnectedTokens));
        Assert.Equal(1, webSocket.DisconnectCount);
    }

    private sealed class FakeRestaurantWebSocketService : IRestaurantWebSocketService
    {
        public event EventHandler<WebSocketEvent>? EventReceived;

        public bool IsConnected { get; private set; }

        public List<string> ConnectedTokens { get; } = [];

        public int DisconnectCount { get; private set; }

        public Task ConnectAsync(string jwt, CancellationToken cancellationToken = default)
        {
            ConnectedTokens.Add(jwt);
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            DisconnectCount++;
            IsConnected = false;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public void Publish(WebSocketEvent websocketEvent)
        {
            EventReceived?.Invoke(this, websocketEvent);
        }
    }
}
