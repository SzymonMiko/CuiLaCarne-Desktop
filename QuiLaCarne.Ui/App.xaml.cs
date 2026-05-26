using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuiLaCarne.Data;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;
using QuiLaCarne.Ui.Views;
using QuiLaCarne.ViewModels;
using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using QuiLaCarne.Ui.Navigation;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace QuiLaCarne.Ui;
public partial class App : Application
{
    public static IHost AppHost { get; private set; }

    public App()
    {
        AppHost =
            Host.CreateDefaultBuilder()
                .ConfigureServices(
                    services =>
                    {
                        services.AddSingleton<HttpClient>();

                        services.AddDbContext<
                            QuiLaCarneDbContext>(options =>
                            {
                                var dbPath =
            Path.Combine(
                Environment.CurrentDirectory,
                "quilacarne.db");

                                var connectionString =
            new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Password = "Admin123!"
            };

                                options.UseSqlite(
            connectionString.ToString());
                            });
                        services.AddSingleton<ReservationService>();
                        services.AddSingleton<AuthService>();
                        services.AddSingleton<DishService>();
                        services.AddSingleton<SyncService>();
                        services.AddSingleton<RestaurantWebSocketService>();
                        services.AddSingleton<MainWindow>();
                        services.AddTransient<LoginPage>();
                        services.AddSingleton<SystemService>();
                        services.AddSingleton<LoginViewModel>();
                        services.AddSingleton<LookupService>();
                        services.AddSingleton<AuthenticationViewModel>();
                        services.AddTransient<Menu>();
                        services.AddTransient<Authentication>();
                        services.AddSingleton<MenuViewModel>();
                        services.AddTransient<UsersPanel>();
                        services.AddSingleton<UsersPanelViewModel>();
                        services.AddSingleton<ManagerViewModel>();
                        services.AddTransient<ManagerTab>();
                        services.AddSingleton<ReportService>();

                        services.AddSingleton<INavigationService, NavigationService>();
                    })
                .Build();
    }
    protected override async void OnStartup(

    StartupEventArgs e)
    {
        Batteries_V2.Init();

     


        await AppHost.StartAsync();

        using (var scope = AppHost.Services.CreateScope())
        {
            var db =
                scope.ServiceProvider
                    .GetRequiredService<
                        QuiLaCarneDbContext>();

            await db.Database.MigrateAsync();
        }

        var mainWindow =
            AppHost.Services.GetRequiredService<LoginPage>();

        mainWindow.Show();

        base.OnStartup(e);
    }
}