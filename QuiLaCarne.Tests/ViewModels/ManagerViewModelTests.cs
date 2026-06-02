using QuiLaCarne.Models;
using QuiLaCarne.Models.Lookup;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Tests.Data;
using QuiLaCarne.Tests.Services;
using Xunit;

namespace QuiLaCarne.Tests.ViewModels;

public sealed class ManagerViewModelTests
{
    [Fact]
    public async Task LoadDishesAsync_LoadsDishesSortedByNameWithAvailability()
    {
        using var database = new TemporarySqliteDatabase();
        var category = new DishesCategories { Token = "CAT_MAIN", Name = "Main" };
        database.Db.Dishes.AddRange(
            new Dishes
            {
                Token = "dish-z",
                Name = "Zupa",
                Price = 12,
                Category = category,
                AvailableFrom = DateTimeOffset.UtcNow.AddDays(1)
            },
            new Dishes
            {
                Token = "dish-a",
                Name = "Burger",
                Price = 30,
                Category = category,
                AvailableFrom = null
            });
        await database.Db.SaveChangesAsync();
        using var httpClient = ServiceTestHelpers.CreateClient(new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson))));
        var viewModel = new ManagerViewModel(
            database.Db,
            new DishService(httpClient),
            new FakeRealtimeUpdateService(),
            new FakeAppDialogService(),
            new FakeUiDispatcherService());

        await viewModel.LoadDishesAsync();

        Assert.Equal(["Burger", "Zupa"], viewModel.Dishes.Select(x => x.Name));
        Assert.True(viewModel.Dishes[0].IsAvailable);
        Assert.False(viewModel.Dishes[1].IsAvailable);
    }

    [Fact]
    public async Task ToggleAvailabilityAsync_WithoutSelectionShowsMessage()
    {
        using var database = new TemporarySqliteDatabase();
        using var httpClient = ServiceTestHelpers.CreateClient(new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson))));
        var dialog = new FakeAppDialogService();
        var viewModel = new ManagerViewModel(
            database.Db,
            new DishService(httpClient),
            new FakeRealtimeUpdateService(),
            dialog,
            new FakeUiDispatcherService());

        await viewModel.ToggleAvailabilityAsync();

        Assert.Single(dialog.Messages);
    }
}
