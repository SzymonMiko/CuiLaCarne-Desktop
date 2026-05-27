using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Ui.Navigation;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public void ShowLogin() => _services.GetRequiredService<LoginPage>().Show();

    public void ShowMenu() => NavigateTo<Menu>("Qui la Carne Panel");

    public void CloseLogin()
    {
        Application.Current.Windows.OfType<LoginPage>().FirstOrDefault()?.Close();
    }

    public void ShowManagerPanel() => NavigateTo<ManagerTab>("Manager Panel");

    public void ShowKitchenMonitor() => NavigateTo<KitchenMonitor>("Kitchen Display System");

    public void ShowUsersPanel() => NavigateTo<UsersPanel>("Users Panel");

    public void ShowTwoFactor() => NavigateTo<Authentication>("Two Factor Authentication");

    public void CloseCurrentWindow()
    {
        Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)?.Close();
    }

    public void ShowPersonnelManagement() => NavigateTo<PersonnelManagement>("Personnel Management");

    public void ShowMenuRoomEditor() => NavigateTo<MenuRoomEditor>("Menu & Room Editor");

    public void ShowSecurityDashboard() => NavigateTo<SecurityDashboard>("Security Dashboard");

    public void ShowIngredientConfirmation() => NavigateTo<IngredientConfirmationPanel>("Ingredient Confirmation");

    public void ShowAdminTwoFactor() => NavigateTo<AdminTwoFactorPanel>("Admin 2FA");

    private void NavigateTo<TPage>(string title)
        where TPage : Page
    {
        var shell = GetShell();
        var page = _services.GetRequiredService<TPage>();

        shell.Navigate(page, title);
        shell.Show();
        shell.Activate();
    }

    private MainWindow GetShell()
    {
        return Application.Current.Windows.OfType<MainWindow>().FirstOrDefault()
            ?? _services.GetRequiredService<MainWindow>();
    }
}
