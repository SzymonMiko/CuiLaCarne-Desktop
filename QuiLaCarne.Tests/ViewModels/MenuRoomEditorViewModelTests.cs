using QuiLaCarne.Models;
using QuiLaCarne.Models.Lookup;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Tests.Data;
using QuiLaCarne.Tests.Services;
using QuiLaCarne.ViewModels;
using Xunit;

namespace QuiLaCarne.Tests.ViewModels;

public sealed class MenuRoomEditorViewModelTests
{
    [Fact]
    public async Task LoadAsync_FiltersDeletedLookupsAndLoadsTableStatus()
    {
        using var database = new TemporarySqliteDatabase();
        database.Db.DishesCategories.AddRange(
            new DishesCategories { Token = "CAT_MAIN", Name = "Main" },
            new DishesCategories { Token = "DELETED_CAT", Name = "DELETED_Old" });
        database.Db.Ingredients.AddRange(
            new Ingredients { Token = "ING_EGG", Name = "Egg" },
            new Ingredients { Token = "DELETED_ING", Name = "Deleted ingredient" });
        database.Db.Allergens.AddRange(
            new Allergens { Token = "ALL_EGG", Name = "Egg allergen" },
            new Allergens { Token = "DELETED_ALL", Name = "Deleted allergen" });
        database.Db.RestaurantTables.Add(new RestaurantTables
        {
            Token = "table-token",
            TableNumber = 3,
            Capacity = 4,
            TableStatus = [new TableStatus { Token = "OCCUPIED", Name = "OCCUPIED" }]
        });
        database.Db.Dishes.Add(new Dishes
        {
            Token = "dish-token",
            Name = "Pizza",
            Price = 40,
            Category = new DishesCategories { Token = "CAT_PIZZA", Name = "Pizza" },
            Ingredients = [new Ingredients { Token = "ING_CHEESE", Name = "Cheese" }]
        });
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();
        using var httpClient = ServiceTestHelpers.CreateClient(new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson))));
        var viewModel = new MenuRoomEditorViewModel(
            database.Db,
            new DishService(httpClient),
            new LookupService(httpClient, database.Db),
            new ReservationService(httpClient),
            new SyncService(httpClient, database.Db),
            new FakeRealtimeUpdateService(),
            new FakeAppDialogService(),
            new FakeFilePickerService(),
            new FakeUiDispatcherService());

        await viewModel.LoadAsync();

        Assert.Contains(viewModel.DishCategories, x => x.Token == "CAT_MAIN");
        Assert.DoesNotContain(viewModel.DishCategories, x => x.Token.StartsWith("DELETED_"));
        Assert.Contains(viewModel.IngredientsForDish, x => x.Token == "ING_EGG");
        Assert.DoesNotContain(viewModel.IngredientsForDish, x => x.Token.StartsWith("DELETED_"));
        Assert.Contains(viewModel.AllergensForIngredient, x => x.Token == "ALL_EGG");
        Assert.DoesNotContain(viewModel.AllergensForIngredient, x => x.Token.StartsWith("DELETED_"));
        Assert.Equal("OCCUPIED", Assert.Single(viewModel.Tables).StatusCode);
        Assert.Equal("Pizza", Assert.Single(viewModel.Dishes).Name);
    }

    [Fact]
    public void SelectingDishCopiesPriceIntoEditedPrice()
    {
        using var database = new TemporarySqliteDatabase();
        using var httpClient = ServiceTestHelpers.CreateClient(new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson))));
        var viewModel = new MenuRoomEditorViewModel(
            database.Db,
            new DishService(httpClient),
            new LookupService(httpClient, database.Db),
            new ReservationService(httpClient),
            new SyncService(httpClient, database.Db),
            new FakeRealtimeUpdateService(),
            new FakeAppDialogService(),
            new FakeFilePickerService(),
            new FakeUiDispatcherService());

        viewModel.SelectedDish = new MenuDishRow
        {
            Token = "dish-token",
            Name = "Pizza",
            Price = 42
        };

        Assert.Equal(42, viewModel.EditedPrice);
    }

    [Fact]
    public void TableMapRow_ComputesStatusStyling()
    {
        var row = new TableMapRow
        {
            StatusCode = "CLEANING",
            IsSelected = false
        };

        Assert.Equal("CLEANING", row.StatusLabel);
        Assert.Equal("#FFF8E1", row.CardBackground);
        Assert.Equal("#D99A00", row.CardBorderBrush);

        row.IsSelected = true;

        Assert.Equal("#111827", row.CardBorderBrush);
        Assert.Equal("3", row.CardBorderThickness);
    }
}
