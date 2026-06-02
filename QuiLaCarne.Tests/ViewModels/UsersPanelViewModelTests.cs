using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Models;
using QuiLaCarne.Models.Lookup;
using QuiLaCarne.Tests.Data;
using QuiLaCarne.ViewModels;
using Xunit;

namespace QuiLaCarne.Tests.ViewModels;

public sealed class UsersPanelViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShowsOnlyClients()
    {
        using var database = new TemporarySqliteDatabase();
        var managerRole = new Roles { Token = "ROLE_MANAGER", Name = "ROLE_MANAGER" };
        var clientRole = new Roles { Token = "ROLE_CLIENT", Name = "ROLE_CLIENT" };
        database.Db.Users.AddRange(
            new Users
            {
                Token = "manager-token",
                Username = "manager",
                Email = "manager@example.com",
                PasswordHash = "hash",
                Roles = [managerRole]
            },
            new Users
            {
                Token = "client-token",
                Username = "client",
                Email = "client@example.com",
                PasswordHash = "hash",
                IsEnabled = true,
                Roles = [clientRole]
            },
            new Users
            {
                Token = "guest-token",
                Username = "guest",
                Email = "guest@example.com",
                PasswordHash = "hash"
            });
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();
        var viewModel = new UsersPanelViewModel(database.Db, new FakeAppDialogService());

        await viewModel.LoadAsync();

        Assert.Equal(["client", "guest"], viewModel.Clients.Select(x => x.Username).OrderBy(x => x));
        Assert.DoesNotContain(viewModel.Clients, x => x.Username == "manager");
    }

    [Fact]
    public async Task LoadAsync_LoadsSelectedClientOrdersReservationsAndReports()
    {
        using var database = new TemporarySqliteDatabase();
        var client = new Users
        {
            Token = "client-token",
            Username = "client",
            Email = "client@example.com",
            PasswordHash = "hash"
        };
        var reporter = new Users
        {
            Token = "reporter-token",
            Username = "manager",
            Email = "manager@example.com",
            PasswordHash = "hash"
        };
        var table = new RestaurantTables
        {
            Token = "table-token",
            TableNumber = 5,
            Capacity = 4
        };
        var dish = new Dishes
        {
            Token = "dish-token",
            Name = "Pizza",
            Price = 40,
            Category = new DishesCategories { Token = "CAT", Name = "Main" }
        };
        var order = new Orders
        {
            Token = "order-token",
            User = client,
            Table = table,
            Items =
            [
                new OrderItems
                {
                    Token = "item-token",
                    Dish = dish,
                    Quantity = 2
                }
            ]
        };
        var reservation = new Reservations
        {
            Token = "reservation-token",
            User = client,
            Table = table,
            ReservedFrom = new DateTimeOffset(2026, 6, 2, 18, 30, 0, TimeSpan.FromHours(2)),
            ReservedUntil = new DateTimeOffset(2026, 6, 2, 20, 0, 0, TimeSpan.FromHours(2)),
            Statuses =
            [
                new ReservationStatus
                {
                    Token = "reservation-status-token",
                    Name = "Confirmed"
                }
            ]
        };
        var report = new GuestReports
        {
            Token = "report-token",
            ReportedUser = client,
            Reporter = reporter,
            Description = "Too loud"
        };
        database.Db.AddRange(client, reporter, table, dish, order, reservation, report);
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();
        var viewModel = new UsersPanelViewModel(database.Db, new FakeAppDialogService());

        await viewModel.LoadAsync();
        await WaitUntilAsync(() =>
            viewModel.ClientOrders.Count == 1 &&
            viewModel.ClientReservations.Count == 1 &&
            viewModel.ClientReports.Count == 1);

        Assert.Equal("2x Pizza", Assert.Single(viewModel.ClientOrders).DishesText);
        Assert.Equal(5, Assert.Single(viewModel.ClientOrders).TableNumber);
        Assert.Equal("2026-06-02 18:30", Assert.Single(viewModel.ClientReservations).ReservedFromText);
        Assert.Equal("2026-06-02 20:00", Assert.Single(viewModel.ClientReservations).ReservedUntilText);
        Assert.Equal(5, Assert.Single(viewModel.ClientReservations).TableNumber);
        Assert.Equal("Confirmed", Assert.Single(viewModel.ClientReservations).StatusesText);
        Assert.Equal("Too loud", Assert.Single(viewModel.ClientReports).Description);
        Assert.Equal("manager", Assert.Single(viewModel.ClientReports).Reporter);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 20; i++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25);
        }

        Assert.True(condition());
    }
}
