using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Services.Api;
using System.Collections.ObjectModel;
using System.Windows;

public partial class SecurityDashboardViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly SystemService _systemService;

    public ObservableCollection<SecurityLogRow> Logs { get; } = new();
    public ObservableCollection<string> Caches { get; } = new();

    private string? selectedCache;
    public string? SelectedCache
    {
        get => selectedCache;
        set => SetProperty(ref selectedCache, value);
    }

    public SecurityDashboardViewModel(QuiLaCarneDbContext db, SystemService systemService)
    {
        _db = db;
        _systemService = systemService;
        _ = LoadLogsAsync();
        _ = LoadCachesAsync();
    }

    [RelayCommand]
    public async Task LoadLogsAsync()
    {
        var logs = await _db.AuditLogs
            .Include(l => l.User)
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(200)
            .ToListAsync();

        Logs.Clear();
        foreach (var log in logs)
        {
            Logs.Add(new SecurityLogRow
            {
                CreatedAt = log.CreatedAt,
                Username = log.User?.Username ?? "system",
                Action = log.Action,
                Details = log.Details ?? "",
                IpAddress = ExtractIp(log.Details)
            });
        }
    }

    private static string ExtractIp(string? details)
    {
        if (string.IsNullOrWhiteSpace(details)) return "-";
        var marker = "ip=";
        var index = details.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return "-";
        var rest = details[(index + marker.Length)..];
        return rest.Split(' ', ';', ',', '|').FirstOrDefault() ?? "-";
    }

    [RelayCommand]
    private async Task LoadCachesAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionService.JwtToken))
        {
            return;
        }

        var caches = await _systemService.GetCacheListAsync(SessionService.JwtToken);

        Caches.Clear();
        foreach (var cache in caches.OrderBy(x => x))
        {
            Caches.Add(cache);
        }

        SelectedCache = Caches.FirstOrDefault();
    }

    [RelayCommand]
    private async Task ClearSelectedCacheAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedCache))
        {
            return;
        }

        var result = MessageBox.Show(
            $"Clear cache {SelectedCache}?",
            "Clear cache",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var cleared = await _systemService.ClearCacheAsync(SessionService.JwtToken, SelectedCache);

        MessageBox.Show(cleared ? "Cache cleared." : "Cache was not cleared.");
        await LoadCachesAsync();
    }

    [RelayCommand]
    private async Task ClearAllCachesAsync()
    {
        var result = MessageBox.Show(
            "Clear all system caches?",
            "Clear all caches",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var cleared = await _systemService.ClearAllCachesAsync(SessionService.JwtToken);

        MessageBox.Show(cleared ? "All caches cleared." : "Caches were not cleared.");
        await LoadCachesAsync();
    }
}

public class SecurityLogRow
{
    public DateTimeOffset CreatedAt { get; set; }
    public string Username { get; set; } = "";
    public string Action { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string Details { get; set; } = "";
}
