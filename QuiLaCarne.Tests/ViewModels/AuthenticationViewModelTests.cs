using System.Net.Http;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Tests.Data;
using QuiLaCarne.Tests.Services;
using QuiLaCarne.ViewModels;
using Xunit;

namespace QuiLaCarne.Tests.ViewModels;

public sealed class AuthenticationViewModelTests
{
    [Fact]
    public async Task Verify2FaCommand_WithoutPendingTokenShowsStatusAndDoesNotCallServer()
    {
        SessionService.PendingTwoFactorToken = "";
        using var database = new TemporarySqliteDatabase();
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var viewModel = CreateViewModel(database, httpClient);

        await viewModel.Verify2FaCommand.ExecuteAsync(null);

        Assert.Contains("Brak oczekującego logowania", viewModel.StatusMessage);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Verify2FaCommand_WithInvalidCodeShowsValidationMessage()
    {
        SessionService.PendingTwoFactorToken = "pre-auth-token";
        using var database = new TemporarySqliteDatabase();
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var viewModel = CreateViewModel(database, httpClient);
        viewModel.TwoFactorCode = "12ab";

        await viewModel.Verify2FaCommand.ExecuteAsync(null);

        Assert.Contains("6 cyfr", viewModel.StatusMessage);
        Assert.Empty(handler.Requests);
        SessionService.PendingTwoFactorToken = "";
    }

    private static AuthenticationViewModel CreateViewModel(
        TemporarySqliteDatabase database,
        HttpClient httpClient)
    {
        return new AuthenticationViewModel(
            new AuthService(httpClient),
            new SyncService(httpClient, database.Db),
            new FakeRealtimeUpdateService(),
            new FakeSessionExpirationService(),
            new FakeNavigationService(),
            new FakeAppDialogService());
    }
}
