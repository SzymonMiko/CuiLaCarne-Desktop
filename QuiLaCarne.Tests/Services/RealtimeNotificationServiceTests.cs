using QuiLaCarne.Models;
using QuiLaCarne.Ui.Services;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class RealtimeNotificationServiceTests
{
    [Fact]
    public void BuildTitle_LabelsKnownWebSocketEvent()
    {
        var title = RealtimeNotificationService.BuildTitle(new WebSocketEvent
        {
            EntityType = "ORDER_ITEM",
            EventType = "UPDATED"
        });

        Assert.Equal("Pozycja zamówienia: zmieniono", title);
    }

    [Fact]
    public void BuildMessage_ShortensLongToken()
    {
        var message = RealtimeNotificationService.BuildMessage(new WebSocketEvent
        {
            EntityType = "INGREDIENT",
            Token = "1234567890abcdef"
        });

        Assert.Equal("Składnik zostało odświeżone z serwera. Token: 12345678...", message);
    }
}
