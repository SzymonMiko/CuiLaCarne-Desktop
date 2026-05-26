using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using System.Collections.ObjectModel;
using System.Windows;

namespace QuiLaCarne.ViewModels;

public partial class KitchenMonitorViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly RestaurantWebSocketService _webSocketService;

    public ObservableCollection<KdsOrderItem> TodoItems { get; } = new();
    public ObservableCollection<KdsOrderItem> InProgressItems { get; } = new();
    public ObservableCollection<KdsOrderItem> ReadyItems { get; } = new();

    public KitchenMonitorViewModel(
        QuiLaCarneDbContext db,
        RestaurantWebSocketService webSocketService)
    {
        _db = db;
        _webSocketService = webSocketService;
        _webSocketService.EventReceived += OnWebSocketEventReceived;
    }

    [RelayCommand]
    public async Task LoadOrdersAsync()
    {
        var items = await _db.OrderItems
            .AsNoTracking()
            .Include(i => i.Dish)
            .Include(i => i.Order)
                .ThenInclude(o => o.Table)
            .Include(i => i.Statuses)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();

        TodoItems.Clear();
        InProgressItems.Clear();
        ReadyItems.Clear();

        foreach (var item in items)
        {
            var status = GetStatusName(item);
            var viewItem = new KdsOrderItem
            {
                Token = item.Token.ToString(),
                DishName = item.Dish?.Name ?? "Unknown dish",
                Quantity = item.Quantity,
                TableNumber = item.Order?.Table?.TableNumber ?? 0,
                Note = item.Note ?? "",
                CreatedAt = item.CreatedAt.DateTime,
                Status = status
            };

            if (IsReady(status))
                ReadyItems.Add(viewItem);
            else if (IsInProgress(status))
                InProgressItems.Add(viewItem);
            else
                TodoItems.Add(viewItem);
        }
    }

    [RelayCommand]
    private async Task StartItemAsync(KdsOrderItem item)
    {
        if (item == null)
            return;

        // TODO: call backend endpoint for changing order item status when your API exposes it.
        // After backend change, WebSocket smart sync should refresh this view.
        MessageBox.Show("Here you call endpoint: change order item status to InProgress.");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ReadyItemAsync(KdsOrderItem item)
    {
        if (item == null)
            return;

        // TODO: call backend endpoint for changing order item status when your API exposes it.
        // After backend change, WebSocket smart sync should refresh this view.
        MessageBox.Show("Here you call endpoint: change order item status to Ready.");
        await Task.CompletedTask;
    }

    private void OnWebSocketEventReceived(WebSocketEvent e)
    {
        if (e.EntityType != "ORDER" &&
            e.EntityType != "ORDER_ITEM" &&
            e.EntityType != "ORDER_STATUS" &&
            e.EntityType != "ORDER_ITEM_STATUS")
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
        return status?.NameEn ?? status?.NamePl ?? "ToDo";
    }

    private static bool IsInProgress(string status)
    {
        var normalized = Normalize(status);
        return normalized.Contains("INPROGRESS") || normalized.Contains("PROGRESS") || normalized.Contains("WTRAKCIE");
    }

    private static bool IsReady(string status)
    {
        var normalized = Normalize(status);
        return normalized.Contains("READY") || normalized.Contains("GOTOWE") || normalized.Contains("DONE");
    }

    private static string Normalize(string value) =>
        value.Replace(" ", "")
             .Replace("_", "")
             .Replace("-", "")
             .ToUpperInvariant();
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
