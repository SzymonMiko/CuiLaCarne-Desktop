using QuiLaCarne.Services.Api;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class ReservationServiceTests
{
    [Fact]
    public async Task AddTableAsync_PostsTableNumberAndCapacity()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new ReservationService(httpClient);

        var added = await service.AddTableAsync("jwt-token", tableNumber: 12, capacity: 4);

        Assert.True(added);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/tables", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);

        var json = await ServiceTestHelpers.ReadJsonAsync(request);
        Assert.Equal(12, json.GetProperty("tableNumber").GetInt32());
        Assert.Equal(4, json.GetProperty("capacity").GetInt32());
    }

    [Fact]
    public async Task DeleteTableAsync_CallsDeleteEndpoint()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new ReservationService(httpClient);

        var deleted = await service.DeleteTableAsync("jwt-token", "table-token");

        Assert.True(deleted);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/api/tables/table-token/delete", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);
    }

    [Fact]
    public async Task GetRestaurantTablesAsync_AddsQueryAndAcceptLanguageHeader()
    {
        var start = DateTimeOffset.Parse("2026-06-01T10:00:00+00:00");
        var end = DateTimeOffset.Parse("2026-06-01T12:00:00+00:00");
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """
                {
                  "success": true,
                  "message": "",
                  "statusCode": 200,
                  "errorMessages": [],
                  "data": [
                    {
                      "token": "table-1",
                      "tableNumber": 1,
                      "capacity": 4,
                      "statusTokens": ["AVAILABLE"],
                      "createdAt": "2026-06-01T10:00:00+00:00",
                      "updatedAt": "2026-06-01T10:00:00+00:00"
                    }
                  ]
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new ReservationService(httpClient);

        var tables = await service.GetRestaurantTablesAsync(
            "jwt-token",
            startTime: start,
            endTime: end,
            language: "en");

        Assert.Equal("table-1", Assert.Single(tables).Token);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("/api/tables?", request.RequestUri?.PathAndQuery);
        Assert.Contains("startTime=", request.RequestUri?.PathAndQuery);
        Assert.Contains("endTime=", request.RequestUri?.PathAndQuery);
        Assert.Equal("en", Assert.Single(request.Headers.GetValues("Accept-Language")));
        ServiceTestHelpers.AssertBearer(request);
    }

    [Fact]
    public async Task GetUserReservationsHistoryAsync_BuildsFilterQuery()
    {
        var from = DateTimeOffset.Parse("2026-06-01T10:00:00+00:00");
        var to = DateTimeOffset.Parse("2026-06-02T10:00:00+00:00");
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """{"success":true,"message":"","statusCode":200,"errorMessages":[],"data":{"items":[],"hasNextPage":false}}""")));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new ReservationService(httpClient);

        var reservations = await service.GetUserReservationsHistoryAsync(
            "jwt-token",
            page: 3,
            size: 7,
            fromDate: from,
            toDate: to,
            statusToken: "PENDING");

        Assert.Empty(reservations);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("/api/reservations?", request.RequestUri?.PathAndQuery);
        Assert.Contains("page=3", request.RequestUri?.PathAndQuery);
        Assert.Contains("size=7", request.RequestUri?.PathAndQuery);
        Assert.Contains("statusToken=PENDING", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);
    }
}
