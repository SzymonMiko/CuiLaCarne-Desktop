using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.ViewModels;

public partial class KitchenMonitorViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly IRealtimeUpdateService _realtimeUpdateService;

    public ObservableCollection<KdsOrderItem> TodoItems { get; } = new();
    public ObservableCollection<KdsOrderItem> InProgressItems { get; } = new();
    public ObservableCollection<KdsOrderItem> ReadyItems { get; } = new();

    public KitchenMonitorViewModel(
        QuiLaCarneDbContext db,
        IRealtimeUpdateService realtimeUpdateService
    )
    {
        _db = db;
        _realtimeUpdateService = realtimeUpdateService;

        _realtimeUpdateService.LocalDataChanged += OnRealtimeDataChanged;

        _ = LoadOrdersAsync();
    }

    [RelayCommand]
    public async Task LoadOrdersAsync()
    {
        var items = await _db
            .OrderItems.AsNoTracking()
            .Include(i => i.Dish)
            .Include(i => i.Order)
                .ThenInclude(o => o.Table)
            .Include(i => i.Statuses)
            .ToListAsync();

        items = items.OrderBy(i => i.CreatedAt).ToList();

        TodoItems.Clear();
        InProgressItems.Clear();
        ReadyItems.Clear();

        foreach (var item in items)
        {
            var status = GetStatusName(item);

            var viewItem = new KdsOrderItem
            {
                Token = item.Token,
                DishName = item.Dish?.Name ?? "Unknown dish",
                Quantity = item.Quantity,
                TableNumber = item.Order?.Table?.TableNumber ?? 0,
                Note = item.Note ?? "",
                CreatedAt = item.CreatedAt.DateTime,
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

    [RelayCommand]
    private void StartItem(KdsOrderItem item)
    {
        if (item == null)
            return;

        /*
         * TODO:
         * Replace this with your real endpoint when available, e.g.
         *
         * await _orderService.ChangeOrderItemStatusAsync(
         *     SessionService.JwtToken,
         *     item.Token,
         *     "IN_PROGRESS_STATUS_TOKEN");
         */

        MessageBox.Show($"TODO: change order item {item.Token} to IN_PROGRESS.");
    }

    [RelayCommand]
    private void ReadyItem(KdsOrderItem item)
    {
        if (item == null)
            return;

        /*
         * TODO:
         * Replace this with your real endpoint when available, e.g.
         *
         * await _orderService.ChangeOrderItemStatusAsync(
         *     SessionService.JwtToken,
         *     item.Token,
         *     "READY_STATUS_TOKEN");
         */

        MessageBox.Show($"TODO: change order item {item.Token} to READY.");
    }

    private void OnRealtimeDataChanged(object? sender, WebSocketEvent e)
    {
        if (
            e.EntityType != "ORDER"
            && e.EntityType != "ORDER_ITEM"
            && e.EntityType != "ORDER_STATUS"
            && e.EntityType != "ORDER_ITEM_STATUS"
        )
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(async () =>
        {
            await LoadOrdersAsync();
        });
    }

    private static string GetStatusName(OrderItems item)
    {
        var status = item.Statuses.FirstOrDefault();

        return status?.Name ?? "ToDo";
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
}
