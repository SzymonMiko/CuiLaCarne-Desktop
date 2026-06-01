using QuiLaCarne.Services.Api;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task CreateGuestReportAsync_PostsClientTokenAndReason()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new ReportService(httpClient);

        var created = await service.CreateGuestReportAsync("jwt-token", "client-token", "bad behavior");

        Assert.True(created);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/report", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);

        var json = await ServiceTestHelpers.ReadJsonAsync(request);
        Assert.Equal("client-token", json.GetProperty("clientToken").GetString());
        Assert.Equal("bad behavior", json.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task ChangeReportStatusAsync_PutsStatusPayload()
    {
        var expiresAt = DateTimeOffset.Parse("2026-06-01T12:00:00+00:00");
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new ReportService(httpClient);

        var changed = await service.ChangeReportStatusAsync(
            "jwt-token",
            "report-token",
            accepted: true,
            expiresAt);

        Assert.True(changed);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("/api/report/change-status", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);

        var json = await ServiceTestHelpers.ReadJsonAsync(request);
        Assert.Equal("report-token", json.GetProperty("reportToken").GetString());
        Assert.True(json.GetProperty("accepted").GetBoolean());
        Assert.Equal(expiresAt, json.GetProperty("expiresAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task GetReportsAsync_ReadsPagedItems()
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
                    "items": [
                      {
                        "token": "report-1",
                        "guestToken": "guest-1",
                        "reporterToken": "manager-1",
                        "reason": "reason",
                        "statusTokens": ["OPEN"],
                        "createdAt": "2026-06-01T10:00:00+00:00",
                        "updatedAt": "2026-06-01T10:00:00+00:00"
                      }
                    ],
                    "hasNextPage": false
                  }
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new ReportService(httpClient);

        var reports = await service.GetReportsAsync("jwt-token", page: 2, size: 5);

        var report = Assert.Single(reports);
        Assert.Equal("report-1", report.Token);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("/api/sync/reports?page=2&size=5", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);
    }
}
