using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Tests.Data;
using QuiLaCarne.Tests.Services;
using Xunit;

namespace QuiLaCarne.Tests.ViewModels;

public sealed class SecurityDashboardViewModelTests
{
    [Fact]
    public async Task LoadLogsAsync_OrdersNewestFirstAndExtractsIpAddress()
    {
        using var database = new TemporarySqliteDatabase();
        var user = new Users
        {
            Token = "user-token",
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hash"
        };
        var systemUser = new Users
        {
            Token = "system-token",
            Username = "system",
            Email = "system@example.com",
            PasswordHash = "hash"
        };
        database.Db.AuditLogs.AddRange(
            new AuditLog
            {
                Token = "old-log",
                User = user,
                Action = "LOGIN",
                Details = "ip=10.0.0.1",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
            },
            new AuditLog
            {
                Token = "new-log",
                User = systemUser,
                Action = "CACHE_CLEAR",
                Details = "ip=192.168.1.4; cache=users",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
            });
        await database.Db.SaveChangesAsync();
        using var httpClient = ServiceTestHelpers.CreateClient(new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json("""{"success":true,"data":[],"message":"","statusCode":200}"""))));
        var viewModel = new SecurityDashboardViewModel(
            database.Db,
            new SystemService(httpClient),
            new FakeAppDialogService());

        await viewModel.LoadLogsAsync();

        Assert.Equal("CACHE_CLEAR", viewModel.Logs[0].Action);
        Assert.Equal("system", viewModel.Logs[0].Username);
        Assert.Equal("192.168.1.4", viewModel.Logs[0].IpAddress);
        Assert.Equal("admin", viewModel.Logs[1].Username);
    }

    [Fact]
    public async Task LoadCachesCommand_LoadsSortedCachesAndSelectsFirst()
    {
        SessionService.JwtToken = "jwt-token";
        using var database = new TemporarySqliteDatabase();
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """{"success":true,"data":["users","audit","menu"],"message":"","statusCode":200}""")));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var viewModel = new SecurityDashboardViewModel(
            database.Db,
            new SystemService(httpClient),
            new FakeAppDialogService());

        await viewModel.LoadCachesCommand.ExecuteAsync(null);

        Assert.Equal(["audit", "menu", "users"], viewModel.Caches);
        Assert.Equal("audit", viewModel.SelectedCache);
        SessionService.JwtToken = "";
    }
}
