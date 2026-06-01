using QuiLaCarne.Models;
using QuiLaCarne.Models.Lookup;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Tests.Data;
using QuiLaCarne.Tests.Services;
using QuiLaCarne.ViewModels;
using Xunit;

namespace QuiLaCarne.Tests.ViewModels;

public sealed class PersonnelManagementViewModelTests
{
    [Fact]
    public void SelectedRole_AdminTogglesIsAdmin()
    {
        using var database = new TemporarySqliteDatabase();
        using var httpClient = ServiceTestHelpers.CreateClient(new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson))));
        var viewModel = new PersonnelManagementViewModel(
            database.Db,
            new UserService(httpClient),
            new SyncService(httpClient, database.Db),
            new FakeRealtimeUpdateService(),
            new FakeAppDialogService(),
            new FakeUiDispatcherService());

        viewModel.SelectedRole = "Admin";

        Assert.True(viewModel.IsAdmin);

        viewModel.SelectedRole = "Staff";

        Assert.False(viewModel.IsAdmin);
    }

    [Fact]
    public async Task LoadAsync_LoadsEmployeesWithSortedRolesText()
    {
        using var database = new TemporarySqliteDatabase();
        database.Db.Users.Add(new Users
        {
            Token = "employee-token",
            Username = "worker",
            Email = "worker@example.com",
            PasswordHash = "hash",
            IsEnabled = true,
            Roles =
            [
                new Roles { Token = "ROLE_STAFF", Name = "Staff" },
                new Roles { Token = "ROLE_ADMIN", Name = "Admin" }
            ]
        });
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();
        using var httpClient = ServiceTestHelpers.CreateClient(new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson))));
        var viewModel = new PersonnelManagementViewModel(
            database.Db,
            new UserService(httpClient),
            new SyncService(httpClient, database.Db),
            new FakeRealtimeUpdateService(),
            new FakeAppDialogService(),
            new FakeUiDispatcherService());

        await viewModel.LoadAsync();

        var employee = Assert.Single(viewModel.Employees);
        Assert.Equal("worker", employee.Username);
        Assert.Equal("Admin, Staff", employee.RolesText);
        Assert.True(employee.IsEnabled);
    }
}
