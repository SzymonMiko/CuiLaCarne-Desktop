using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Ui.Navigation;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;
    private readonly ILocalizationService _localization;

    public NavigationService(IServiceProvider services, ILocalizationService localization)
    {
        _services = services;
        _localization = localization;
    }

    public void ShowLogin() => _services.GetRequiredService<LoginPage>().Show();

    public void ShowMenu() => NavigateTo<Menu>("Panel Qui la Carne");

    public void CloseLogin()
    {
        Application.Current.Windows.OfType<LoginPage>().FirstOrDefault()?.Close();
    }

    public void ShowManagerPanel() => NavigateTo<MenuRoomEditor>("Panel zarządzania");

    public void ShowKitchenMonitor() => NavigateTo<KitchenMonitor>("Monitor kuchni");

    public void ShowUsersPanel() => NavigateTo<UsersPanel>("Panel klientów");

    public void ShowTwoFactor() => NavigateTo<Authentication>("Uwierzytelnianie dwuetapowe");

    public void CloseCurrentWindow()
    {
        Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)?.Close();
    }

    public void ShowPersonnelManagement() => NavigateTo<PersonnelManagement>("Zarządzanie personelem");

    public void ShowMenuRoomEditor() => NavigateTo<MenuRoomEditor>("Panel zarządzania");

    public void ShowSecurityDashboard() => NavigateTo<SecurityDashboard>("Panel bezpieczeństwa");

    public void ShowIngredientConfirmation() => NavigateTo<IngredientConfirmationPanel>("Potwierdzenie składnika");

    public void ShowAdminTwoFactor() => NavigateTo<AdminTwoFactorPanel>("Administracyjne 2FA");

    private void NavigateTo<TPage>(string title)
        where TPage : Page
    {
        var shell = GetShell();
        var page = _services.GetRequiredService<TPage>();
        RoutedEventHandler? loadedHandler = null;
        loadedHandler = (_, _) =>
        {
            page.Loaded -= loadedHandler;
            _localization.RefreshCurrentView();
        };
        page.Loaded += loadedHandler;

        shell.Navigate(page, _localization.Translate(title));
        shell.Show();
        shell.Activate();
        _localization.RefreshCurrentView();
    }

    private MainWindow GetShell()
    {
        return Application.Current.Windows.OfType<MainWindow>().FirstOrDefault()
            ?? _services.GetRequiredService<MainWindow>();
    }
}
