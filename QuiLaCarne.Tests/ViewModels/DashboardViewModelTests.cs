using QuiLaCarne.Models;
using QuiLaCarne.Tests.Data;
using QuiLaCarne.ViewModels;
using Xunit;

namespace QuiLaCarne.Tests.ViewModels;

public sealed class DashboardViewModelTests
{
    [Fact]
    public async Task LoadAsync_LoadsUsersIntoCollection()
    {
        using var database = new TemporarySqliteDatabase();
        database.Db.Users.AddRange(
            new Users
            {
                Token = "user-1",
                Username = "anna",
                Email = "anna@example.com",
                PasswordHash = "hash"
            },
            new Users
            {
                Token = "user-2",
                Username = "bartek",
                Email = "bartek@example.com",
                PasswordHash = "hash"
            });
        await database.Db.SaveChangesAsync();
        var viewModel = new DashboardViewModel(database.Db);

        await viewModel.LoadAsync();

        Assert.Equal(["anna", "bartek"], viewModel.Users.Select(x => x.Username).OrderBy(x => x));
    }
}
