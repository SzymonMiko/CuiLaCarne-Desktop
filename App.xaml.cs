using System.Net.Http;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuiLaCarne.Data;
using QuiLaCarne.Services;
using QuiLaCarne.ViewModels;
using QuiLaCarne.Ui; // Added to reference MainWindow (from your current file)

namespace QuiLaCarne;

public partial class App : Application
{
    public static ServiceProvider ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        // 1. Register your services, DB, and ViewModels (from STEP 8)
        services.AddSingleton<HttpClient>();
        services.AddDbContext<QuiLaCarneDbContext>(options =>
        {
            options.UseSqlite("Data Source=quilacarne.db");
        });
        
        services.AddSingleton<AuthService>();
        services.AddSingleton<SyncService>();
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<DashboardViewModel>();

        // 2. Register your Windows/Views
        services.AddTransient<MainWindow>();

        // 3. Build the provider
        ServiceProvider = services.BuildServiceProvider();

        // 4. Resolve the starting window and SHOW it
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}