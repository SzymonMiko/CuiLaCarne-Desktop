using QuiLaCarne.Services.Api;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class LookupServiceTests
{
    [Fact]
    public async Task AddIngredientAsync_PostsNamesAndAllergenTokens()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        using var db = ServiceTestHelpers.CreateDbContext();
        var service = new LookupService(httpClient, db);

        var added = await service.AddIngredientAsync(
            "jwt-token",
            "Jajka",
            "Eggs",
            ["ALLERGEN_EGG"]);

        Assert.True(added);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/ingredients", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);

        var json = await ServiceTestHelpers.ReadJsonAsync(request);
        Assert.Equal("Jajka", json.GetProperty("entity").GetProperty("namePl").GetString());
        Assert.Equal("Eggs", json.GetProperty("entity").GetProperty("nameEn").GetString());
        Assert.Equal("ALLERGEN_EGG", json.GetProperty("allergenTokens")[0].GetString());
    }

    [Fact]
    public async Task GetAllergensAsync_SendsAcceptLanguageAndReadsWrapperItems()
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
                    "item": [
                      {
                        "token": "ALLERGEN_EGG",
                        "name": "Jajka",
                        "namePl": "Jajka",
                        "nameEn": "Eggs",
                        "createdAt": "2026-06-01T10:00:00+00:00",
                        "updatedAt": "2026-06-01T10:00:00+00:00"
                      }
                    ]
                  }
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        using var db = ServiceTestHelpers.CreateDbContext();
        var service = new LookupService(httpClient, db);

        var allergens = await service.GetAllergensAsync("jwt-token", language: "en");

        var allergen = Assert.Single(allergens);
        Assert.Equal("ALLERGEN_EGG", allergen.Token);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/dishes/allergens/dictionary", request.RequestUri?.PathAndQuery);
        Assert.Equal("en", Assert.Single(request.Headers.GetValues("Accept-Language")));
        ServiceTestHelpers.AssertBearer(request);
    }

    [Theory]
    [InlineData("api/tables/status/add", "Available", "Available")]
    [InlineData("api/order/status/add", "Pending", "Pending")]
    [InlineData("api/order/item/status/add", "Ready", "Ready")]
    [InlineData("api/dishes/category/add", "Pizza", "Pizza")]
    [InlineData("api/dishes/allergens/add", "Gluten", "Gluten")]
    public async Task AddDictionaryItemMethods_PostNames(
        string expectedPath,
        string namePl,
        string nameEn)
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        using var db = ServiceTestHelpers.CreateDbContext();
        var service = new LookupService(httpClient, db);

        var added = expectedPath switch
        {
            "api/tables/status/add" => await service.AddTableStatusAsync("jwt-token", namePl, nameEn),
            "api/order/status/add" => await service.AddOrderStatusAsync("jwt-token", namePl, nameEn),
            "api/order/item/status/add" => await service.AddOrderItemStatusAsync("jwt-token", namePl, nameEn),
            "api/dishes/category/add" => await service.AddDishCategoryAsync("jwt-token", namePl, nameEn),
            "api/dishes/allergens/add" => await service.AddAllergenAsync("jwt-token", namePl, nameEn),
            _ => false
        };

        Assert.True(added);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"/{expectedPath}", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);

        var json = await ServiceTestHelpers.ReadJsonAsync(request);
        Assert.Equal(namePl, json.GetProperty("namePl").GetString());
        Assert.Equal(nameEn, json.GetProperty("nameEn").GetString());
    }

    [Theory]
    [InlineData("table-status", "/api/tables/status/TOKEN")]
    [InlineData("order-status", "/api/order/status/TOKEN")]
    [InlineData("order-item-status", "/api/order/item/status/TOKEN")]
    [InlineData("ingredient", "/api/ingredients/TOKEN")]
    [InlineData("allergen", "/api/dishes/allergen/TOKEN")]
    [InlineData("dish-category", "/api/dishes/category/TOKEN")]
    public async Task DeleteDictionaryItemMethods_CallExpectedEndpoint(
        string target,
        string expectedPath)
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        using var db = ServiceTestHelpers.CreateDbContext();
        var service = new LookupService(httpClient, db);

        var deleted = target switch
        {
            "table-status" => await service.DeleteTableStatusAsync("jwt-token", "TOKEN"),
            "order-status" => await service.DeleteOrderStatusAsync("jwt-token", "TOKEN"),
            "order-item-status" => await service.DeleteOrderItemStatusAsync("jwt-token", "TOKEN"),
            "ingredient" => await service.DeleteIngredientAsync("jwt-token", "TOKEN"),
            "allergen" => await service.DeleteAllergenAsync("jwt-token", "TOKEN"),
            "dish-category" => await service.DeleteDishCategoryAsync("jwt-token", "TOKEN"),
            _ => false
        };

        Assert.True(deleted);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal(expectedPath, request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);
    }

    [Fact]
    public async Task GetAllDictionariesAsync_ReadsApiResponseData()
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
                    "allergens": [],
                    "dishCategories": [],
                    "banStatuses": [],
                    "reportStatuses": [],
                    "orderStatuses": [],
                    "orderItemStatuses": [],
                    "reservationStatuses": [],
                    "tableStatuses": []
                  }
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        using var db = ServiceTestHelpers.CreateDbContext();
        var service = new LookupService(httpClient, db);

        var dictionaries = await service.GetAllDictionariesAsync("jwt-token");

        Assert.NotNull(dictionaries);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("/api/sync/dictionaries", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);
    }
}
