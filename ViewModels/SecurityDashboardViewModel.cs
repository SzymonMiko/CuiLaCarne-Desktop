using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using System.Collections.ObjectModel;

public partial class SecurityDashboardViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    public ObservableCollection<SecurityLogRow> Logs { get; } = new();

    public SecurityDashboardViewModel(QuiLaCarneDbContext db)
    {
        _db = db;
        _ = LoadLogsAsync();
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
}

public class SecurityLogRow
{
    public DateTimeOffset CreatedAt { get; set; }
    public string Username { get; set; } = "";
    public string Action { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string Details { get; set; } = "";
}
