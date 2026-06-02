using QuiLaCarne.Models;
using QuiLaCarne.Models.Lookup;
using QuiLaCarne.Tests.Data;
using QuiLaCarne.ViewModels;
using Xunit;

namespace QuiLaCarne.Tests.ViewModels;

public sealed class KitchenMonitorViewModelTests
{
    [Fact]
    public async Task LoadOrdersAsync_GroupsItemsByKitchenStatus()
    {
        using var database = new TemporarySqliteDatabase();
        var table = new RestaurantTables
        {
            Token = "table-token",
            TableNumber = 7,
            Capacity = 4
        };
        var user = new Users
        {
            Token = "user-token",
            Username = "client",
            Email = "client@example.com",
            PasswordHash = "hash"
        };
        var category = new DishesCategories { Token = "CAT_MAIN", Name = "Main" };
        var order = new Orders
        {
            Token = "order-token",
            User = user,
            Table = table,
            CreatedAt = DateTimeOffset.Now.AddMinutes(-10),
            Items =
            [
                new OrderItems
                {
                    Token = "todo-token",
                    Dish = new Dishes { Token = "dish-todo", Name = "Soup", Price = 10, Category = category },
                    Quantity = 1,
                    CreatedAt = DateTimeOffset.Now.AddMinutes(-9)
                },
                new OrderItems
                {
                    Token = "progress-token",
                    Dish = new Dishes { Token = "dish-progress", Name = "Steak", Price = 60, Category = category },
                    Quantity = 2,
                    CreatedAt = DateTimeOffset.Now.AddMinutes(-8),
                    Statuses = [new OrderItemsStatus { Token = "IN_PROGRESS", Name = "IN_PROGRESS" }]
                },
                new OrderItems
                {
                    Token = "ready-token",
                    Dish = new Dishes { Token = "dish-ready", Name = "Dessert", Price = 20, Category = category },
                    Quantity = 3,
                    CreatedAt = DateTimeOffset.Now.AddMinutes(-7),
                    Statuses = [new OrderItemsStatus { Token = "READY", Name = "READY" }]
                }
            ]
        };
        database.Db.Orders.Add(order);
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();
        var viewModel = new KitchenMonitorViewModel(
            database.Db,
            new FakeRealtimeUpdateService(),
            new FakeAppDialogService(),
            new FakeUiDispatcherService());

        await viewModel.LoadOrdersAsync();

        Assert.Equal("Soup", Assert.Single(viewModel.TodoItems).DishName);
        Assert.Equal("Steak", Assert.Single(viewModel.InProgressItems).DishName);
        Assert.Equal("Dessert", Assert.Single(viewModel.ReadyItems).DishName);
        Assert.Equal(7, Assert.Single(viewModel.ReadyItems).TableNumber);
    }

    [Fact]
    public void KdsOrderItem_FormatsDisplayText()
    {
        var item = new KdsOrderItem
        {
            Quantity = 2,
            TableNumber = 0,
            CreatedAt = DateTime.Now.AddMinutes(-5),
            ReservationAt = new DateTime(2026, 6, 2, 18, 30, 0)
        };

        Assert.Equal("Ilość: 2", item.QuantityText);
        Assert.Equal("Stolik: -", item.TableText);
        Assert.StartsWith("Czas oczekiwania:", item.WaitingText);
        Assert.Equal("Rezerwacja: 18:30", item.ReservationTimeText);
    }
}
