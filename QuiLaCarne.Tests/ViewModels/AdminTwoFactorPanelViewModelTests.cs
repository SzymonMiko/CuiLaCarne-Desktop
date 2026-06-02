using QuiLaCarne.Services.Api;
using QuiLaCarne.Tests.Services;
using Xunit;

namespace QuiLaCarne.Tests.ViewModels;

public sealed class AdminTwoFactorPanelViewModelTests
{
    [Fact]
    public async Task GenerateTwoFactorCommand_StoresQrCodeAndManualKey()
    {
        SessionService.JwtToken = "jwt-token";
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """
                {
                  "success": true,
                  "data": {
                    "qrCodeImageUrl": "otpauth://example",
                    "manualEntryKey": "ABC123"
                  },
                  "message": "",
                  "statusCode": 200
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var viewModel = new AdminTwoFactorPanelViewModel(
            new AuthService(httpClient),
            new FakeAppDialogService());

        await viewModel.GenerateTwoFactorCommand.ExecuteAsync(null);

        Assert.Equal("otpauth://example", viewModel.QrCodeImageUrl);
        Assert.Equal("ABC123", viewModel.ManualEntryKey);
        ServiceTestHelpers.AssertBearer(Assert.Single(handler.Requests));
        SessionService.JwtToken = "";
    }

    [Fact]
    public async Task EnableTwoFactorCommand_WithBlankCodeShowsValidationMessage()
    {
        using var httpClient = ServiceTestHelpers.CreateClient(new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson))));
        var dialog = new FakeAppDialogService();
        var viewModel = new AdminTwoFactorPanelViewModel(
            new AuthService(httpClient),
            dialog);

        await viewModel.EnableTwoFactorCommand.ExecuteAsync(null);

        Assert.Single(dialog.Messages);
    }
}
