using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuiLaCarne.Data;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;
using QuiLaCarne.Ui.Navigation;
using QuiLaCarne.Ui.Services;
using QuiLaCarne.ViewModels;
using SQLitePCL;

namespace QuiLaCarne.Ui;

public partial class App : Application
{
    public static IHost AppHost { get; private set; }

    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<SessionExpirationService>();
                services.AddSingleton<ISessionExpirationService>(serviceProvider =>
                    serviceProvider.GetRequiredService<SessionExpirationService>());
                services.AddSingleton(serviceProvider =>
                    {
                        var sessionExpirationService =
                            serviceProvider.GetRequiredService<SessionExpirationService>();

                        sessionExpirationService.InnerHandler = new HttpClientHandler();

                        return new HttpClient(sessionExpirationService)
                        {
                            BaseAddress = new Uri("https://api.quilacarne.com.pl/")
                        };
                    });

                services.AddDbContext<QuiLaCarneDbContext>(options =>
                {
                    var dbPath = Path.Combine(Environment.CurrentDirectory, "quilacarne.db");

                    var connectionString = new SqliteConnectionStringBuilder
                    {
                        DataSource = dbPath,
                        Password = "Admin123!",
                    };

                    options.UseSqlite(connectionString.ToString());
                });

                services.AddTransient<AuthService>();
                services.AddTransient<SystemService>();
                services.AddTransient<SyncService>();
                services.AddTransient<LookupService>();
                services.AddTransient<DishService>();
                services.AddTransient<ReservationService>();
                services.AddTransient<UserService>();
                services.AddTransient<ReportService>();
                services.AddSingleton<MainWindow>();
                services.AddTransient<Menu>();
                services.AddTransient<ManagerTab>();
                services.AddTransient<KitchenMonitor>();
                services.AddTransient<UsersPanel>();
                services.AddTransient<Authentication>();
                services.AddTransient<PersonnelManagement>();
                services.AddTransient<MenuRoomEditor>();
                services.AddTransient<SecurityDashboard>();
                services.AddTransient<IngredientConfirmationPanel>();
                services.AddTransient<AdminTwoFactorPanel>();
                

                services.AddSingleton<IRestaurantWebSocketService, RestaurantWebSocketService>();
                services.AddSingleton<IRealtimeUpdateService, RealtimeUpdateService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IAppDialogService, WpfDialogService>();
                services.AddSingleton<IFilePickerService, WpfFilePickerService>();
                services.AddSingleton<IUiDispatcherService, WpfUiDispatcherService>();
                services.AddSingleton<ILocalizationService, WpfLocalizationService>();
                services.AddSingleton<RealtimeNotificationService>();



                services.AddTransient<UsersPanelViewModel>();
                services.AddTransient<AuthenticationViewModel>();
                services.AddTransient<AdminTwoFactorPanelViewModel>();
                services.AddTransient<IngredientConfirmationPanelViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<MenuViewModel>();
                services.AddTransient<ManagerViewModel>();
                services.AddTransient<KitchenMonitorViewModel>();
                services.AddTransient<PersonnelManagementViewModel>();
                services.AddTransient<MenuRoomEditorViewModel>();
                services.AddTransient<SecurityDashboardViewModel>();
                services.AddTransient<QuiLaCarne.Ui.LoginPage>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        var culture = new CultureInfo("pl-PL");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        Batteries_V2.Init();

        await AppHost.StartAsync();

        AppHost.Services
            .GetRequiredService<ISessionExpirationService>()
            .SessionExpired += OnSessionExpired;
        AppHost.Services.GetRequiredService<RealtimeNotificationService>();

        using (var scope = AppHost.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuiLaCarneDbContext>();

            await db.Database.MigrateAsync();
        }

        var mainWindow =
     AppHost.Services.GetRequiredService<QuiLaCarne.Ui.LoginPage>();

        mainWindow.Show();

        base.OnStartup(e);
    }

    private void OnSessionExpired(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() => _ = HandleSessionExpiredAsync());
    }

    private async Task HandleSessionExpiredAsync()
    {
        try
        {
            await AppHost.Services
                .GetRequiredService<IRealtimeUpdateService>()
                .StopAsync();
        }
        catch
        {
            // The login screen should still appear even if the realtime socket is already closed.
        }

        var navigation = AppHost.Services.GetRequiredService<INavigationService>();

        if (!Windows.OfType<LoginPage>().Any())
        {
            navigation.ShowLogin();
        }

        foreach (var window in Windows.OfType<MainWindow>().ToList())
        {
            window.Close();
        }

        MessageBox.Show(
            "Sesja wygasła. Zaloguj się ponownie.",
            "Sesja wygasła",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
