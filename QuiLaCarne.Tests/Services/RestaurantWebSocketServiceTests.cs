using QuiLaCarne.Services.Api;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class RestaurantWebSocketServiceTests
{
    [Fact]
    public async Task ConnectAsync_WithBlankJwtThrowsBeforeOpeningSocket()
    {
        await using var service = new RestaurantWebSocketService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.ConnectAsync(""));
    }
}
