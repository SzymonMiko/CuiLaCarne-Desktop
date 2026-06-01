using System.Net;
using System.Net.Http.Headers;
using QuiLaCarne.Services.Api;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class SessionExpirationServiceTests
{
    [Fact]
    public void Clear_ResetsSessionState()
    {
        SessionService.JwtToken = "jwt-token";
        SessionService.RefreshToken = "refresh-token";
        SessionService.IsAdmin = true;

        var service = new SessionExpirationService();

        service.Clear();

        Assert.Equal("", SessionService.JwtToken);
        Assert.Equal("", SessionService.RefreshToken);
        Assert.False(SessionService.IsAdmin);
    }

    [Fact]
    public async Task SendAsync_RaisesSessionExpiredOnceWhenAuthorizedRequestGetsUnauthorized()
    {
        var service = new SessionExpirationService
        {
            InnerHandler = new FakeHttpMessageHandler(_ =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)))
        };
        using var httpClient = new HttpClient(service)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var raised = 0;
        service.SessionExpired += (_, _) => raised++;
        var request = new HttpRequestMessage(HttpMethod.Get, "api/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "jwt-token");

        await httpClient.SendAsync(request);

        Assert.Equal(1, raised);
        Assert.Equal("", SessionService.JwtToken);
        Assert.Equal("", SessionService.RefreshToken);
        Assert.False(SessionService.IsAdmin);

        service.NotifyExpired();
        Assert.Equal(1, raised);
    }
}
