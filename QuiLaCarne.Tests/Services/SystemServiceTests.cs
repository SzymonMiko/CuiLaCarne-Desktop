using QuiLaCarne.Services.Api;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class SystemServiceTests
{
    [Fact]
    public async Task GetCacheListAsync_ReturnsCachesAndSendsBearer()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """{"success":true,"data":["users","menu"],"message":"","statusCode":200,"errorMessages":[]}""")));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new SystemService(httpClient);

        var caches = await service.GetCacheListAsync("jwt-token");

        Assert.Equal(["users", "menu"], caches);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/system/cache/list", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);
    }

    [Fact]
    public async Task ClearCacheAsync_CallsNamedCacheDeleteEndpoint()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new SystemService(httpClient);

        var cleared = await service.ClearCacheAsync("jwt-token", "menu");

        Assert.True(cleared);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/api/system/cache/clear/menu", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);
    }

    [Fact]
    public async Task ClearAllCachesAsync_CallsClearAllEndpoint()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new SystemService(httpClient);

        var cleared = await service.ClearAllCachesAsync("jwt-token");

        Assert.True(cleared);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/api/system/cache/clear-all", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);
    }
}
