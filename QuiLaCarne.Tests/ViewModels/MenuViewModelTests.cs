using QuiLaCarne.Services.IServices;
using QuiLaCarne.ViewModels;
using Xunit;

namespace QuiLaCarne.Tests.ViewModels;

public sealed class MenuViewModelTests
{
    [Fact]
    public void ToggleLanguageCommand_RefreshesLanguageSwitchText()
    {
        var localization = new FakeLocalizationService();
        var viewModel = new MenuViewModel(new FakeNavigationService(), localization);

        viewModel.ToggleLanguageCommand.Execute(null);

        Assert.Equal(1, localization.ToggleCount);
        Assert.Equal("Polski", viewModel.LanguageSwitchText);
    }

    [Fact]
    public void NavigationCommands_CallExpectedNavigationMethods()
    {
        var navigation = new FakeNavigationService();
        var viewModel = new MenuViewModel(navigation, new FakeLocalizationService());

        viewModel.OpenManagerCommand.Execute(null);
        viewModel.OpenKitchenCommand.Execute(null);
        viewModel.OpenUsersCommand.Execute(null);
        viewModel.OpenPersonnelCommand.Execute(null);
        viewModel.OpenSecurityCommand.Execute(null);
        viewModel.OpenIngredientConfirmationCommand.Execute(null);
        viewModel.OpenAdminTwoFactorCommand.Execute(null);
        viewModel.OpenTwoFactorCommand.Execute(null);

        Assert.Equal(
            [
                nameof(INavigationService.ShowManagerPanel),
                nameof(INavigationService.ShowKitchenMonitor),
                nameof(INavigationService.ShowUsersPanel),
                nameof(INavigationService.ShowPersonnelManagement),
                nameof(INavigationService.ShowSecurityDashboard),
                nameof(INavigationService.ShowIngredientConfirmation),
                nameof(INavigationService.ShowAdminTwoFactor),
                nameof(INavigationService.ShowTwoFactor)
            ],
            navigation.Calls);
    }
}
