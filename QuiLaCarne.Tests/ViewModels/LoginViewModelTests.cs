using System.Net.Http;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Tests.Data;
using QuiLaCarne.Tests.Services;
using QuiLaCarne.ViewModels;
using Xunit;

namespace QuiLaCarne.Tests.ViewModels;

public sealed class LoginViewModelTests
{
    [Fact]
    public async Task LoginAsync_WhenTwoFactorIsRequiredStoresPendingTokenAndNavigates()
    {
        ResetSession();
        using var database = new TemporarySqliteDatabase();
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """
                {
                  "success": true,
                  "data": {
                    "token": "pre-auth-token",
                    "refreshToken": "",
                    "username": "admin",
                    "roles": [],
                    "requires2fa": true
                  },
                  "message": "",
                  "statusCode": 200
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var navigation = new FakeNavigationService();
        var viewModel = CreateViewModel(database, httpClient, navigation);
        viewModel.Username = "admin";
        viewModel.Password = "secret";

        await viewModel.LoginAsync();

        Assert.Equal("pre-auth-token", SessionService.PendingTwoFactorToken);
        Assert.Equal("admin", SessionService.PendingTwoFactorUsername);
        Assert.Equal(
            [nameof(FakeNavigationService.ShowTwoFactor), nameof(FakeNavigationService.CloseLogin)],
            navigation.Calls);
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsNotAdminShowsAccessDenied()
    {
        ResetSession();
        using var database = new TemporarySqliteDatabase();
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """
                {
                  "success": true,
                  "data": {
                    "token": "jwt-token",
                    "refreshToken": "refresh-token",
                    "username": "client",
                    "roles": ["ROLE_CLIENT"],
                    "requires2fa": false
                  },
                  "message": "",
                  "statusCode": 200
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var dialog = new FakeAppDialogService();
        var viewModel = CreateViewModel(database, httpClient, new FakeNavigationService(), dialog);

        await viewModel.LoginAsync();

        Assert.Contains(dialog.Messages, x => x.Contains("Brak dostępu"));
        Assert.Equal("", SessionService.JwtToken);
    }

    private static LoginViewModel CreateViewModel(
        TemporarySqliteDatabase database,
        HttpClient httpClient,
        FakeNavigationService navigation,
        FakeAppDialogService? dialog = null)
    {
        return new LoginViewModel(
            navigation,
            new FakeRealtimeUpdateService(),
            new ReservationService(httpClient),
            new DishService(httpClient),
            new SystemService(httpClient),
            new AuthService(httpClient),
            new SyncService(httpClient, database.Db),
            new LookupService(httpClient, database.Db),
            new FakeSessionExpirationService(),
            dialog ?? new FakeAppDialogService());
    }

    private static void ResetSession()
    {
        SessionService.JwtToken = "";
        SessionService.RefreshToken = "";
        SessionService.Username = "";
        SessionService.PendingTwoFactorToken = "";
        SessionService.PendingTwoFactorUsername = "";
        SessionService.IsAdmin = false;
    }
}
