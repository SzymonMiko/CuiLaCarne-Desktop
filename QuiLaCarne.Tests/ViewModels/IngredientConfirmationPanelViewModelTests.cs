using QuiLaCarne.Models;
using QuiLaCarne.Models.Lookup;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Tests.Data;
using QuiLaCarne.Tests.Services;
using Xunit;

namespace QuiLaCarne.Tests.ViewModels;

public sealed class IngredientConfirmationPanelViewModelTests
{
    [Fact]
    public async Task LoadIngredientsAsync_FiltersDeletedIngredientsAndSortsByName()
    {
        using var database = new TemporarySqliteDatabase();
        database.Db.Ingredients.AddRange(
            new Ingredients { Token = "ING_TOMATO", Name = "Tomato" },
            new Ingredients { Token = "ING_APPLE", Name = "Apple" },
            new Ingredients { Token = "DELETED_ING", Name = "Deleted" },
            new Ingredients { Token = "ING_OLD", Name = "DELETED_Old" });
        await database.Db.SaveChangesAsync();
        var viewModel = CreateViewModel(database);

        await viewModel.LoadIngredientsAsync();

        Assert.Equal(["Apple", "Tomato"], viewModel.Ingredients.Select(x => x.Name));
    }

    [Fact]
    public async Task LoadAffectedDishesAsync_LoadsDishesContainingSelectedIngredient()
    {
        using var database = new TemporarySqliteDatabase();
        var ingredient = new Ingredients { Token = "ING_CHEESE", Name = "Cheese" };
        var category = new DishesCategories { Token = "CAT_MAIN", Name = "Main" };
        database.Db.Dishes.AddRange(
            new Dishes
            {
                Token = "DISH_PIZZA",
                Name = "Pizza",
                Price = 35,
                Category = category,
                Ingredients = [ingredient]
            },
            new Dishes
            {
                Token = "DISH_SOUP",
                Name = "Soup",
                Price = 15,
                Category = category,
                Ingredients = [new Ingredients { Token = "ING_CARROT", Name = "Carrot" }]
            });
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();
        var viewModel = CreateViewModel(database);
        viewModel.SelectedIngredient = ingredient;

        await viewModel.LoadAffectedDishesAsync();

        var dish = Assert.Single(viewModel.AffectedDishes);
        Assert.Equal("Pizza", dish.Name);
        Assert.True(dish.IsAvailable);
    }

    [Fact]
    public async Task ConfirmMissingIngredientCommand_WithBlankReasonShowsValidationMessage()
    {
        using var database = new TemporarySqliteDatabase();
        var viewModel = CreateViewModel(database, out var dialog);
        viewModel.SelectedIngredient = new Ingredients { Token = "ING_CHEESE", Name = "Cheese" };
        viewModel.Reason = " ";

        await viewModel.ConfirmMissingIngredientCommand.ExecuteAsync(null);

        Assert.Single(dialog.Messages);
    }

    private static IngredientConfirmationPanelViewModel CreateViewModel(
        TemporarySqliteDatabase database)
    {
        return CreateViewModel(database, out _);
    }

    private static IngredientConfirmationPanelViewModel CreateViewModel(
        TemporarySqliteDatabase database,
        out FakeAppDialogService dialog)
    {
        var httpClient = ServiceTestHelpers.CreateClient(new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson))));
        dialog = new FakeAppDialogService();

        return new IngredientConfirmationPanelViewModel(
            database.Db,
            new LookupService(httpClient, database.Db),
            new FakeRealtimeUpdateService(),
            dialog,
            new FakeUiDispatcherService());
    }
}
