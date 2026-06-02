using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;
using System.Windows.Threading;

namespace QuiLaCarne.ViewModels;

public partial class KitchenMonitorViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly IRealtimeUpdateService _realtimeUpdateService;
    private readonly IAppDialogService _dialog;
    private readonly IUiDispatcherService _uiDispatcher;
    private readonly DispatcherTimer _refreshTimer;
    private bool _isLoading;

    public ObservableCollection<KdsOrderItem> TodoItems { get; } = new();
    public ObservableCollection<KdsOrderItem> InProgressItems { get; } = new();
    public ObservableCollection<KdsOrderItem> ReadyItems { get; } = new();

    public KitchenMonitorViewModel(
        QuiLaCarneDbContext db,
        IRealtimeUpdateService realtimeUpdateService,
        IAppDialogService dialog,
        IUiDispatcherService uiDispatcher
    )
    {
        _db = db;
        _realtimeUpdateService = realtimeUpdateService;
        _dialog = dialog;
        _uiDispatcher = uiDispatcher;

        _realtimeUpdateService.LocalDataChanged += OnRealtimeDataChanged;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _refreshTimer.Tick += async (_, _) => await LoadOrdersAsync();
        _refreshTimer.Start();

        _ = LoadOrdersAsync();
    }

    [RelayCommand]
    public async Task LoadOrdersAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;

        try
        {
        var now = DateTimeOffset.Now;

        var items = await _db
            .OrderItems.AsNoTracking()
            .Include(i => i.Dish)
            .Include(i => i.Order)
                .ThenInclude(o => o.Table)
            .Include(i => i.Statuses)
            .ToListAsync();

        var reservations = await _db
            .Reservations.AsNoTracking()
            .ToListAsync();

        TodoItems.Clear();
        InProgressItems.Clear();
        ReadyItems.Clear();

        var visibleItems = items
            .Select(item => new
            {
                Item = item,
                Reservation = FindMatchingReservation(item.Order, reservations),
            })
            .Where(x => ShouldShowInKitchen(x.Item, x.Reservation, now))
            .OrderBy(x => x.Reservation?.ReservedFrom ?? x.Item.CreatedAt)
            .ThenBy(x => x.Item.CreatedAt)
            .ToList();

        foreach (var entry in visibleItems)
        {
            var item = entry.Item;
            var status = GetStatusName(item);

            var viewItem = new KdsOrderItem
            {
                Token = item.Token,
                DishName = item.Dish?.Name ?? "Nieznane danie",
                Quantity = item.Quantity,
                TableNumber = item.Order?.Table?.TableNumber ?? 0,
                Note = item.Note ?? "",
                CreatedAt = item.CreatedAt.LocalDateTime,
                ReservationAt = entry.Reservation?.ReservedFrom.LocalDateTime,
                Status = status,
            };

            if (IsReady(status))
            {
                ReadyItems.Add(viewItem);
            }
            else if (IsInProgress(status))
            {
                InProgressItems.Add(viewItem);
            }
            else
            {
                TodoItems.Add(viewItem);
            }
        }
        }
        finally
        {
            _isLoading = false;
        }
    }


    private void OnRealtimeDataChanged(object? sender, WebSocketEvent e)
    {
        if (
            e.EntityType != "ORDER"
            && e.EntityType != "ORDER_ITEM"
            && e.EntityType != "ORDER_STATUS"
            && e.EntityType != "ORDER_ITEM_STATUS"
            && e.EntityType != "RESERVATION"
        )
        {
            return;
        }

        _ = _uiDispatcher.InvokeAsync(LoadOrdersAsync);
    }

    private static string GetStatusName(OrderItems item)
    {
        var status = item.Statuses.FirstOrDefault();

        return status?.Name ?? "Do zrobienia";
    }

    private static Reservations? FindMatchingReservation(
        Orders? order,
        IEnumerable<Reservations> reservations)
    {
        if (order == null)
        {
            return null;
        }

        return reservations
            .Where(reservation =>
                reservation.TableId == order.TableId &&
                reservation.UserId == order.UserId &&
                (reservation.ReservedUntil == null || order.CreatedAt <= reservation.ReservedUntil))
            .OrderBy(reservation => reservation.ReservedFrom)
            .FirstOrDefault();
    }

    private static bool ShouldShowInKitchen(
        OrderItems item,
        Reservations? reservation,
        DateTimeOffset now)
    {
        if (IsReady(GetStatusName(item)))
        {
            return true;
        }

        if (reservation == null)
        {
            return true;
        }

        return now >= reservation.ReservedFrom.AddMinutes(-30);
    }

    private static bool IsInProgress(string status)
    {
        var normalized = Normalize(status);

        return normalized.Contains("INPROGRESS")
            || normalized.Contains("PROGRESS")
            || normalized.Contains("WTRAKCIE");
    }

    private static bool IsReady(string status)
    {
        var normalized = Normalize(status);

        return normalized.Contains("READY")
            || normalized.Contains("GOTOWE")
            || normalized.Contains("DONE");
    }

    private static string Normalize(string value)
    {
        return value.Replace(" ", "").Replace("_", "").Replace("-", "").ToUpperInvariant();
    }
}

public partial class KdsOrderItem : ObservableObject
{
    public string Token { get; set; } = "";

    public string DishName { get; set; } = "";

    public int Quantity { get; set; }

    public int TableNumber { get; set; }

    public string Note { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public DateTime? ReservationAt { get; set; }

    public string Status { get; set; } = "";

    public string QuantityText => $"Ilość: {Quantity}";

    public string TableText => TableNumber <= 0 ? "Stolik: -" : $"Stolik: {TableNumber}";

    public string WaitingText
    {
        get
        {
            var minutes = Math.Max(0, (int)(DateTime.Now - CreatedAt).TotalMinutes);

            return $"Czas oczekiwania: {minutes} min";
        }
    }

    public string OrderTimeText => $"Zamówienie: {CreatedAt:HH:mm}";

    public string ReservationTimeText =>
        ReservationAt.HasValue ? $"Rezerwacja: {ReservationAt.Value:HH:mm}" : "";
}
