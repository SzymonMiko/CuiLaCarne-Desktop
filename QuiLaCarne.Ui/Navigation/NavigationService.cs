using Microsoft.Extensions.DependencyInjection;
using QuiLaCarne.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QuiLaCarne.Ui.Navigation;
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public void ShowLogin() =>
        _services.GetRequiredService<LoginPage>().Show();

    public void ShowMenu()
    {
        var menu = _services.GetRequiredService<Menu>();
        menu.Show();
        menu.Activate();
    }
    public void CloseLogin()
    {
        Application.Current.Windows
            .OfType<LoginPage>()
            .FirstOrDefault()
            ?.Close();
    }
    public void ShowManagerPanel()
    {
        var window = new Window
        {
            Title = "Manager Panel",
            Content = _services.GetRequiredService<ManagerTab>(),
            Width = 1000,
            Height = 650
        };

        window.Show();
    }

    public void ShowUsersPanel() =>
        _services.GetRequiredService<UsersPanel>().Show();

    public void ShowTwoFactor() =>
        _services.GetRequiredService<Authentication>().Show();

    public void CloseCurrentWindow()
    {
        Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive)
            ?.Close();
    }
}