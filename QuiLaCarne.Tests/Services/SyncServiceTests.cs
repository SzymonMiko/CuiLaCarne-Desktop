using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Models;
using QuiLaCarne.Models.Lookup;
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

    [Fact]
    public async Task SyncUsersAsync_UpdatesExistingUserTokenSoOrdersCanLink()
    {
        using var database = new TemporarySqliteDatabase();
        database.Db.Users.Add(new Users
        {
            Token = "local-old-token",
            Username = "client",
            Email = "client@example.pl",
            PasswordHash = "hash",
            IsEnabled = true
        });
        database.Db.RestaurantTables.Add(new RestaurantTables
        {
            Token = "table-token",
            TableNumber = 4,
            Capacity = 2
        });
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();

        var handler = new FakeHttpMessageHandler(request =>
        {
            var path = request.RequestUri?.PathAndQuery;
            var json = path switch
            {
                "/api/sync/users?page=1" =>
                    """
                    {
                      "success": true,
                      "data": {
                        "items": [
                          {
                            "token": "server-user-token",
                            "username": "client",
                            "email": "client@example.pl",
                            "isActive": true,
                            "roleTokens": [],
                            "createdAt": "2026-01-01T00:00:00Z",
                            "updatedAt": "2026-01-01T00:00:00Z"
                          }
                        ],
                        "hasNextPage": false
                      },
                      "message": "",
                      "statusCode": 200
                    }
                    """,
                "/api/sync/orders?page=1" =>
                    """
                    {
                      "success": true,
                      "data": {
                        "items": [
                          {
                            "token": "order-token",
                            "tableToken": "table-token",
                            "userToken": "server-user-token",
                            "createdAt": "2026-01-02T10:00:00Z",
                            "updatedAt": "2026-01-02T10:00:00Z"
                          }
                        ],
                        "hasNextPage": false
                      },
                      "message": "",
                      "statusCode": 200
                    }
                    """,
                _ => throw new InvalidOperationException($"Unexpected request: {path}")
            };

            return Task.FromResult(FakeHttpMessageHandler.Json(json));
        });
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new SyncService(httpClient, database.Db);

        await service.SyncUsersAsync("jwt-token");
        await service.SyncOrdersAsync("jwt-token");

        var user = await database.Db.Users.SingleAsync();
        var order = await database.Db.Orders.Include(x => x.User).SingleAsync();
        Assert.Equal("server-user-token", user.Token);
        Assert.Equal("client", order.User.Username);
    }

    [Fact]
    public async Task SyncOrdersAsync_AcceptsClientTokenAlias()
    {
        using var database = new TemporarySqliteDatabase();
        database.Db.Users.Add(new Users
        {
            Token = "client-token",
            Username = "client",
            Email = "client@example.pl",
            PasswordHash = "hash",
            IsEnabled = true
        });
        database.Db.RestaurantTables.Add(new RestaurantTables
        {
            Token = "table-token",
            TableNumber = 4,
            Capacity = 2
        });
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();

        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """
                {
                  "success": true,
                  "data": {
                    "items": [
                      {
                        "orderToken": "order-token",
                        "tableToken": "table-token",
                        "clientToken": "client-token",
                        "createdAt": "2026-01-02T10:00:00Z",
                        "updatedAt": "2026-01-02T10:00:00Z"
                      }
                    ],
                    "hasNextPage": false
                  },
                  "message": "",
                  "statusCode": 200
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new SyncService(httpClient, database.Db);

        await service.SyncOrdersAsync("jwt-token");

        var order = await database.Db.Orders.Include(x => x.User).SingleAsync();
        Assert.Equal("order-token", order.Token);
        Assert.Equal("client", order.User.Username);
    }

    [Fact]
    public async Task SyncReservationsAsync_AcceptsReservationAndClientTokenAliases()
    {
        using var database = new TemporarySqliteDatabase();
        database.Db.Users.Add(new Users
        {
            Token = "client-token",
            Username = "client",
            Email = "client@example.pl",
            PasswordHash = "hash",
            IsEnabled = true
        });
        database.Db.RestaurantTables.Add(new RestaurantTables
        {
            Token = "table-token",
            TableNumber = 4,
            Capacity = 2
        });
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();

        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """
                {
                  "success": true,
                  "data": {
                    "items": [
                      {
                        "reservationToken": "reservation-token",
                        "tableToken": "table-token",
                        "clientToken": "client-token",
                        "startTime": "2026-01-02T10:00:00Z",
                        "endTime": "2026-01-02T12:00:00Z",
                        "statusTokens": [],
                        "createdAt": "2026-01-01T10:00:00Z",
                        "updatedAt": "2026-01-01T10:00:00Z"
                      }
                    ],
                    "hasNextPage": false
                  },
                  "message": "",
                  "statusCode": 200
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new SyncService(httpClient, database.Db);

        await service.SyncReservationsAsync("jwt-token");

        var reservation = await database.Db.Reservations.Include(x => x.User).SingleAsync();
        Assert.Equal("reservation-token", reservation.Token);
        Assert.Equal("client", reservation.User.Username);
        Assert.Equal(new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero), reservation.ReservedFrom);
    }

    [Fact]
    public async Task SyncOrderItemsAsync_AcceptsReservationAndMenuItemTokenAliases()
    {
        using var database = new TemporarySqliteDatabase();
        var category = new DishesCategories { Token = "CAT_MAIN", Name = "Main" };
        var client = new Users
        {
            Token = "client-token",
            Username = "client",
            Email = "client@example.pl",
            PasswordHash = "hash",
            IsEnabled = true
        };
        var table = new RestaurantTables
        {
            Token = "table-token",
            TableNumber = 4,
            Capacity = 2
        };
        database.Db.Orders.Add(new Orders
        {
            Token = "reservation-token",
            User = client,
            Table = table
        });
        database.Db.Dishes.Add(new Dishes
        {
            Token = "menu-item-token",
            Name = "Pizza",
            Price = 30,
            Category = category
        });
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();

        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """
                {
                  "success": true,
                  "data": {
                    "items": [
                      {
                        "orderItemToken": "order-item-token",
                        "reservationToken": "reservation-token",
                        "menuItemToken": "menu-item-token",
                        "quantity": 2,
                        "createdAt": "2026-01-02T10:00:00Z",
                        "updatedAt": "2026-01-02T10:00:00Z"
                      }
                    ],
                    "hasNextPage": false
                  },
                  "message": "",
                  "statusCode": 200
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new SyncService(httpClient, database.Db);

        await service.SyncOrderItemsAsync("jwt-token");

        var item = await database.Db.OrderItems.Include(x => x.Dish).SingleAsync();
        Assert.Equal("order-item-token", item.Token);
        Assert.Equal("Pizza", item.Dish.Name);
        Assert.Equal(2, item.Quantity);
    }
}
