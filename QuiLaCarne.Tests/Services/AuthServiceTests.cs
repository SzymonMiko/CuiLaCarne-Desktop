using System.Text.Json;
using QuiLaCarne.Services.Api;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Verify2FaAsync_SendsPreAuthTokenAndCodeAsText()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """
                {
                  "success": true,
                  "message": "",
                  "statusCode": 200,
                  "errorMessages": [],
                  "data": {
                    "token": "jwt-token",
                    "refreshToken": "refresh-token",
                    "username": "manager",
                    "roles": ["ROLE_MANAGER"],
                    "requires2fa": false
                  }
                }
                """)));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new AuthService(httpClient);

        var loginData = await service.Verify2FaAsync("pre-auth-token", "012345");

        Assert.NotNull(loginData);
        Assert.Equal("jwt-token", loginData.Token);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/auth/verify-2fa", request.RequestUri?.PathAndQuery);

        var body = await request.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        Assert.Equal("pre-auth-token", root.GetProperty("preAuthToken").GetString());
        Assert.Equal("012345", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task LoginAsync_PostsUsernameAndPasswordToLoginEndpoint()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """
                {
                  "success": true,
                  "message": "",
                  "statusCode": 200,
                  "errorMessages": [],
                  "data": {
                    "token": "jwt-token",
                    "refreshToken": "refresh-token",
                    "username": "manager",
                    "roles": ["ROLE_MANAGER"],
                    "requires2fa": false
                  }
                }
                """)));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new AuthService(httpClient);

        var loginData = await service.LoginAsync("manager", "secret");

        Assert.NotNull(loginData);
        Assert.Equal("manager", loginData.Username);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/auth/login", request.RequestUri?.PathAndQuery);

        var body = await request.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        Assert.Equal("manager", root.GetProperty("username").GetString());
        Assert.Equal("secret", root.GetProperty("password").GetString());
    }

    [Fact]
    public async Task Enable2FaAsync_SendsBearerTokenAndCode()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """{"success":true,"data":null,"message":"","statusCode":200,"errorMessages":[]}""")));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new AuthService(httpClient);

        var enabled = await service.Enable2FaAsync("jwt-token", "123456");

        Assert.True(enabled);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/user/2fa/enable", request.RequestUri?.PathAndQuery);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("jwt-token", request.Headers.Authorization?.Parameter);

        var body = await request.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        Assert.Equal("123456", json.RootElement.GetProperty("code").GetString());
    }
}
