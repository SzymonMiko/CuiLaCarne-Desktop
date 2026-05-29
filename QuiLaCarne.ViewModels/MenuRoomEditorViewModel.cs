using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;
using System.Collections.ObjectModel;
using System.Windows;

namespace QuiLaCarne.ViewModels;

public partial class MenuRoomEditorViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly DishService _dishService;
    private readonly ReservationService _reservationService;
    private readonly IRealtimeUpdateService _realtimeUpdateService;

    public ObservableCollection<MenuDishRow> Dishes { get; } = new();

    public ObservableCollection<TableMapRow> Tables { get; } = new();

    private MenuDishRow? selectedDish;
    public MenuDishRow? SelectedDish
    {
        get => selectedDish;
        set
        {
            if (SetProperty(ref selectedDish, value))
            {
                EditedPrice = value?.Price ?? 0;
            }
        }
    }

    private decimal editedPrice;
    public decimal EditedPrice
    {
        get => editedPrice;
        set => SetProperty(ref editedPrice, value);
    }

    private string blockReason = "";
    public string BlockReason
    {
        get => blockReason;
        set => SetProperty(ref blockReason, value);
    }

    private int newTableNumber;
    public int NewTableNumber
    {
        get => newTableNumber;
        set => SetProperty(ref newTableNumber, value);
    }

    private int newTableCapacity = 2;
    public int NewTableCapacity
    {
        get => newTableCapacity;
        set => SetProperty(ref newTableCapacity, value);
    }

    public MenuRoomEditorViewModel(
        QuiLaCarneDbContext db,
        DishService dishService,
        ReservationService reservationService,
        IRealtimeUpdateService realtimeUpdateService)
    {
        _db = db;
        _dishService = dishService;
        _reservationService = reservationService;
        _realtimeUpdateService = realtimeUpdateService;
        _realtimeUpdateService.LocalDataChanged += OnRealtimeDataChanged;

        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var dishes = await _db.Dishes
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();

        Dishes.Clear();

        foreach (var d in dishes)
        {
            Dishes.Add(new MenuDishRow
            {
                Token = d.Token,
                Name = d.Name,
                Price = d.Price,
                IsAvailable =
                    d.AvailableFrom == null ||
                    d.AvailableFrom <= DateTimeOffset.UtcNow
            });
        }

        var tables = await _db.RestaurantTables
            .AsNoTracking()
            .OrderBy(t => t.TableNumber)
            .ToListAsync();

        Tables.Clear();

        foreach (var t in tables)
        {
            Tables.Add(new TableMapRow
            {
                Token = t.Token,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity
            });
        }
    }

    [RelayCommand]
    private async Task SavePriceAsync()
    {
        if (SelectedDish == null)
        {
            return;
        }

        await _dishService.EditDishAsync(
            SessionService.JwtToken,
            dishToken: SelectedDish.Token,
            price: (int)EditedPrice);

        MessageBox.Show("Price change sent. The editor will refresh after the server confirms it.");
    }

    [RelayCommand]
    private async Task BlockDishAsync()
    {
        if (SelectedDish == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(BlockReason))
        {
            MessageBox.Show("Reason is required when blocking dish.");
            return;
        }

        await _dishService.ChangeDishAvailabilityAsync(
            SessionService.JwtToken,
            SelectedDish.Token,
            false,
            BlockReason);

        MessageBox.Show("Dish block request sent. The editor will refresh after the server confirms it.");
    }

    [RelayCommand]
    private async Task AddTableAsync()
    {
        if (NewTableNumber <= 0 ||
            NewTableCapacity <= 0)
        {
            return;
        }

        await _reservationService.AddTableAsync(
            SessionService.JwtToken,
            NewTableNumber,
            NewTableCapacity);

        NewTableNumber = 0;
        NewTableCapacity = 2;

        MessageBox.Show("Table change sent. The editor will refresh after the server confirms it.");
    }

    private void OnRealtimeDataChanged(object? sender, WebSocketEvent e)
    {
        var entityType = e.EntityType.ToUpperInvariant();

        if (entityType != "DISH" &&
            entityType != "DISH_AVAILABILITY" &&
            entityType != "MENU_AVAILABILITY" &&
            entityType != "INGREDIENT" &&
            entityType != "CATEGORY" &&
            entityType != "TABLE" &&
            entityType != "TABLE_STATUS")
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(async () =>
        {
            await LoadAsync();
        });
    }
}

public class MenuDishRow
{
    public string Token { get; set; } = "";

    public string Name { get; set; } = "";

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; }
}

public class TableMapRow
{
    public string Token { get; set; } = "";

    public int TableNumber { get; set; }

    public int Capacity { get; set; }
}
