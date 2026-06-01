using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;
using System.Collections.ObjectModel;

public partial class SecurityDashboardViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly SystemService _systemService;
    private readonly IAppDialogService _dialog;

    public ObservableCollection<SecurityLogRow> Logs { get; } = new();
    public ObservableCollection<string> Caches { get; } = new();

    private string? selectedCache;
    public string? SelectedCache
    {
        get => selectedCache;
        set => SetProperty(ref selectedCache, value);
    }

    public SecurityDashboardViewModel(
        QuiLaCarneDbContext db,
        SystemService systemService,
        IAppDialogService dialog)
    {
        _db = db;
        _systemService = systemService;
        _dialog = dialog;
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

        var confirmed = _dialog.Confirm(
            $"Wyczyścić cache {SelectedCache}?",
            "Wyczyść cache");

        if (!confirmed)
        {
            return;
        }

        var cleared = await _systemService.ClearCacheAsync(SessionService.JwtToken, SelectedCache);

        _dialog.ShowMessage(cleared ? "Cache wyczyszczony." : "Cache nie został wyczyszczony.");
        await LoadCachesAsync();
    }

    [RelayCommand]
    private async Task ClearAllCachesAsync()
    {
        var confirmed = _dialog.Confirm(
            "Wyczyścić wszystkie cache systemu?",
            "Wyczyść wszystkie cache");

        if (!confirmed)
        {
            return;
        }

        var cleared = await _systemService.ClearAllCachesAsync(SessionService.JwtToken);

        _dialog.ShowMessage(cleared ? "Wszystkie cache wyczyszczone." : "Cache nie zostały wyczyszczone.");
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
