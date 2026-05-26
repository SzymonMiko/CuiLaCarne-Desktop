using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Services.Api;
using System.Collections.ObjectModel;
using System.Windows;

public partial class MenuRoomEditorViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly DishService _dishService;
    private readonly MenuRoomService _menuRoomService;
    private readonly SyncService _syncService;

    public ObservableCollection<MenuDishRow> Dishes { get; } = new();
    public ObservableCollection<TableMapRow> Tables { get; } = new();

    [ObservableProperty] private MenuDishRow? selectedDish;
    [ObservableProperty] private decimal editedPrice;
    [ObservableProperty] private string blockReason = "";
    [ObservableProperty] private int newTableNumber;
    [ObservableProperty] private int newTableCapacity = 2;

    public MenuRoomEditorViewModel(QuiLaCarneDbContext db, DishService dishService, MenuRoomService menuRoomService, SyncService syncService)
    {
        _db = db;
        _dishService = dishService;
        _menuRoomService = menuRoomService;
        _syncService = syncService;
        _ = LoadAsync();
    }

    partial void OnSelectedDishChanged(MenuDishRow? value)
    {
        EditedPrice = value?.Price ?? 0;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var dishes = await _db.Dishes.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
        Dishes.Clear();
        foreach (var d in dishes)
        {
            Dishes.Add(new MenuDishRow
            {
                Token = d.Token,
                Name = d.Name,
                Price = d.Price,
                IsAvailable = d.AvailableFrom == null || d.AvailableFrom <= DateTimeOffset.UtcNow
            });
        }

        var tables = await _db.RestaurantTables.AsNoTracking().OrderBy(t => t.TableNumber).ToListAsync();
        Tables.Clear();
        foreach (var t in tables)
        {
            Tables.Add(new TableMapRow { Token = t.Token, TableNumber = t.TableNumber, Capacity = t.Capacity });
        }
    }

    [RelayCommand]
    private async Task SavePriceAsync()
    {
        if (SelectedDish == null) return;
        await _menuRoomService.ChangeDishPriceAsync(SessionService.JwtToken, SelectedDish.Token, EditedPrice);
        await _syncService.SyncDishesAsync(SessionService.JwtToken);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task BlockDishAsync()
    {
        if (SelectedDish == null) return;
        if (string.IsNullOrWhiteSpace(BlockReason))
        {
            MessageBox.Show("Reason is required when blocking dish.");
            return;
        }

        await _dishService.ChangeDishAvailabilityAsync(SessionService.JwtToken, SelectedDish.Token, false, BlockReason);
        await _syncService.SyncDishesAsync(SessionService.JwtToken);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddTableAsync()
    {
        if (NewTableNumber <= 0 || NewTableCapacity <= 0) return;
        await _menuRoomService.AddTableAsync(SessionService.JwtToken, NewTableNumber, NewTableCapacity);
        await _syncService.SyncTablesAsync(SessionService.JwtToken);
        await LoadAsync();
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
