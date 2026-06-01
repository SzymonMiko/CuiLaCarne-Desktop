using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Services.IServices;
using System.Collections.ObjectModel;

namespace QuiLaCarne.ViewModels;

public partial class UsersPanelViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly IAppDialogService _dialog;

    public ObservableCollection<ClientRow> Clients { get; } = [];

    public ObservableCollection<ClientOrderRow> ClientOrders { get; } = [];

    public ObservableCollection<ClientReportRow> ClientReports { get; } = [];

    private ClientRow? selectedClient;
    public ClientRow? SelectedClient
    {
        get => selectedClient;
        set
        {
            if (SetProperty(ref selectedClient, value))
            {
                _ = LoadClientDetailsAsync(value);
            }
        }
    }

    public UsersPanelViewModel(QuiLaCarneDbContext db, IAppDialogService dialog)
    {
        _db = db;
        _dialog = dialog;
    }

    public async Task LoadAsync()
    {
        var selectedClientToken = SelectedClient?.Token;

        var users =
            await _db.Users
                .AsNoTracking()
                .Include(x => x.Roles)
                .OrderBy(x => x.Username)
                .ToListAsync();

        Clients.Clear();

        foreach (var user in users.Where(IsClient))
        {
            Clients.Add(new ClientRow
            {
                Id = user.Id,
                Token = user.Token,
                Username = user.Username,
                Email = user.Email,
                IsEnabled = user.IsEnabled,
                UpdatedAt = user.UpdatedAt.LocalDateTime,
                RolesText = user.Roles.Count == 0
                    ? ""
                    : string.Join(", ", user.Roles.Select(x => x.Name).OrderBy(x => x))
            });
        }

        SelectedClient =
            Clients.FirstOrDefault(x => x.Token == selectedClientToken) ??
            Clients.FirstOrDefault();
    }

    private async Task LoadClientDetailsAsync(ClientRow? client)
    {
        try
        {
            ClientOrders.Clear();
            ClientReports.Clear();

            if (client == null)
            {
                return;
            }

            var orders = await _db.Orders
                .AsNoTracking()
                .Include(x => x.Table)
                .Include(x => x.Statuses)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Dish)
                .Where(x => x.UserId == client.Id)
                .ToListAsync();

            foreach (var order in orders.OrderByDescending(x => x.CreatedAt))
            {
                ClientOrders.Add(new ClientOrderRow
                {
                    CreatedAt = order.CreatedAt.LocalDateTime,
                    TableNumber = order.Table.TableNumber,
                    StatusesText = order.Statuses.Count == 0
                        ? ""
                        : string.Join(", ", order.Statuses.Select(x => x.Name).OrderBy(x => x)),
                    DishesText = order.Items.Count == 0
                        ? "Brak dań"
                        : string.Join(", ", order.Items
                            .OrderBy(x => x.Dish.Name)
                            .Select(x => $"{x.Quantity}x {x.Dish.Name}"))
                });
            }

            var reports = await _db.GuestReports
                .AsNoTracking()
                .Include(x => x.Reporter)
                .Include(x => x.Statuses)
                .Where(x => x.ReportedUserId == client.Id)
                .ToListAsync();

            foreach (var report in reports.OrderByDescending(x => x.CreatedAt))
            {
                ClientReports.Add(new ClientReportRow
                {
                    CreatedAt = report.CreatedAt.LocalDateTime,
                    Reporter = report.Reporter.Username,
                    Description = report.Description,
                    StatusesText = report.Statuses.Count == 0
                        ? ""
                        : string.Join(", ", report.Statuses.Select(x => x.Name).OrderBy(x => x))
                });
            }
        }
        catch (Exception ex)
        {
            _dialog.ShowMessage($"Nie udało się załadować szczegółów klienta.\n{ex.Message}");
        }
    }

    private static bool IsClient(Users user)
    {
        var roleNames = user.Roles
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (roleNames.Count == 0)
        {
            return true;
        }

        return !roleNames.Any(IsEmployeeRole);
    }

    private static bool IsEmployeeRole(string role)
    {
        var normalized = role.Replace("ROLE_", "", StringComparison.OrdinalIgnoreCase);

        return normalized.Contains("ADMIN", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("MANAGER", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("STAFF", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("EMPLOYEE", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("WAITER", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("KITCHEN", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("COOK", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("PERSONNEL", StringComparison.OrdinalIgnoreCase);
    }
}

public class ClientRow
{
    public Guid Id { get; set; }

    public string Token { get; set; } = "";

    public string Username { get; set; } = "";

    public string Email { get; set; } = "";

    public bool IsEnabled { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string RolesText { get; set; } = "";
}

public class ClientOrderRow
{
    public DateTime CreatedAt { get; set; }

    public int TableNumber { get; set; }

    public string StatusesText { get; set; } = "";

    public string DishesText { get; set; } = "";

    public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd HH:mm");
}

public class ClientReportRow
{
    public DateTime CreatedAt { get; set; }

    public string Reporter { get; set; } = "";

    public string Description { get; set; } = "";

    public string StatusesText { get; set; } = "";

    public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd HH:mm");
}
