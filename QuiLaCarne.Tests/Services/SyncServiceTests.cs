using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Tests.Data;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class SyncServiceTests
{
    [Fact]
    public async Task SyncRolesAsync_UpsertsRolesIntoLocalDatabase()
    {
        using var database = new TemporarySqliteDatabase();
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """
                {
                  "success": true,
                  "message": "",
                  "statusCode": 200,
                  "errorMessages": [],
                  "data": [
                    { "token": "ROLE_MANAGER", "name": "Manager" }
                  ]
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new SyncService(httpClient, database.Db);

        await service.SyncRolesAsync("jwt-token");

        var role = await database.Db.Roles.SingleAsync();
        Assert.Equal("ROLE_MANAGER", role.Token);
        Assert.Equal("Manager", role.Name);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/sync/roles", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);
    }

    [Fact]
    public async Task SyncDictionariesAsync_UpsertsDictionaryRows()
    {
        using var database = new TemporarySqliteDatabase();
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """
                {
                  "success": true,
                  "message": "",
                  "statusCode": 200,
                  "errorMessages": [],
                  "data": {
                    "allergens": [
                      { "token": "ALLERGEN_GLUTEN", "namePl": "Gluten", "nameEn": "Gluten" }
                    ],
                    "dishCategories": [
                      { "token": "CAT_PIZZA", "namePl": "Pizza", "nameEn": "Pizza" }
                    ],
                    "banStatuses": [],
                    "reportStatuses": [],
                    "orderStatuses": [],
                    "orderItemStatuses": [],
                    "reservationStatuses": [],
                    "tableStatuses": [
                      { "token": "AVAILABLE", "namePl": "Dostepny", "nameEn": "Available" }
                    ]
                  }
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new SyncService(httpClient, database.Db);

        await service.SyncDictionariesAsync("jwt-token");

        Assert.Equal("Gluten", (await database.Db.Allergens.SingleAsync()).Name);
        Assert.Equal("Pizza", (await database.Db.DishesCategories.SingleAsync()).Name);
        Assert.Equal("Dostepny", (await database.Db.TableStatuses.SingleAsync()).Name);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/api/sync/dictionaries", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);
    }
}
