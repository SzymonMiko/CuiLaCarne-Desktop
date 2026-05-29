using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;
using System.Collections.ObjectModel;
using System.Windows;

public partial class ManagerViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly DishService _dishService;
    private readonly IRealtimeUpdateService _realtimeUpdateService;

    [ObservableProperty]
    private ObservableCollection<DishManagerItem> dishes = new();

    [ObservableProperty]
    private DishManagerItem? selectedDish;

    [ObservableProperty]
    private string unavailableReason = "Test WebSocket";

    public ManagerViewModel(
        QuiLaCarneDbContext db,
        DishService dishService,
        IRealtimeUpdateService realtimeUpdateService)
    {
        _db = db;
        _dishService = dishService;
        _realtimeUpdateService = realtimeUpdateService;
        _realtimeUpdateService.LocalDataChanged += OnRealtimeDataChanged;
    }

    [RelayCommand]
    public async Task LoadDishesAsync()
    {
        var dishesFromDb = await _db.Dishes
            .AsNoTracking()
            .OrderBy(dish => dish.Name)
            .ToListAsync();

        Dishes.Clear();

        foreach (var dish in dishesFromDb)
        {
            Dishes.Add(new DishManagerItem
            {
                Token = dish.Token,
                Name = dish.Name,
                IsAvailable = dish.AvailableFrom == null ||
                    dish.AvailableFrom <= DateTimeOffset.UtcNow
            });
        }
    }

    [RelayCommand]
    public async Task ToggleAvailabilityAsync()
    {
        if (SelectedDish == null)
        {
            MessageBox.Show("Choose dish first.");
            return;
        }

        var newAvailable = !SelectedDish.IsAvailable;

        await _dishService.ChangeDishAvailabilityAsync(
            SessionService.JwtToken,
            SelectedDish.Token,
            newAvailable,
            newAvailable ? "" : UnavailableReason);

        MessageBox.Show("Change sent. The list will refresh after the server confirms it.");
    }

    private void OnRealtimeDataChanged(object? sender, WebSocketEvent e)
    {
        var entityType = e.EntityType.ToUpperInvariant();

        if (entityType != "DISH" &&
            entityType != "DISH_AVAILABILITY" &&
            entityType != "MENU_AVAILABILITY" &&
            entityType != "INGREDIENT" &&
            entityType != "CATEGORY")
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(async () =>
        {
            await LoadDishesAsync();
        });
    }
}

public partial class DishManagerItem : ObservableObject
{
    public string Token { get; set; } = "";

    public string Name { get; set; } = "";

    [ObservableProperty]
    private bool isAvailable;
}
