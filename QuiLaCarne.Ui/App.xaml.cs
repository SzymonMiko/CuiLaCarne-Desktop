using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuiLaCarne.Data;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Ui.Views;
using QuiLaCarne.ViewModels;
using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

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

                                options.UseSqlite(
                                    $"Data Source={dbPath}");
                            });
                        services.AddSingleton<ReservationService>();
                        services.AddSingleton<AuthService>();
                        services.AddSingleton<DishService>();
                        services.AddSingleton<SyncService>();
                        services.AddSingleton<MainWindow>();
                        services.AddSingleton<LoginPage>();
                        services.AddSingleton<SystemService>();
                        services.AddSingleton<LoginViewModel>();
                        services.AddSingleton<LookupService>();
                        services.AddSingleton<AuthenticationViewModel>();
                        services.AddSingleton<Menu>();
                        services.AddSingleton<Authentication>();
                        services.AddSingleton<MenuViewModel>();
                    })
                .Build();
    }
    protected override async void OnStartup(
    StartupEventArgs e)
    {
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
