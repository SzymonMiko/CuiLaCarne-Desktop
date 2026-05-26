using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


public partial class ManagerViewModel : ObservableObject
{
    private readonly DishService _dishService;
    private readonly SyncService _syncService;
    private readonly RestaurantWebSocketService _webSocketService;

    [ObservableProperty]
    private ObservableCollection<DishManagerItem> dishes = new();

    [ObservableProperty]
    private DishManagerItem? selectedDish;

    [ObservableProperty]
    private string unavailableReason = "Test WebSocket";

    public ManagerViewModel(
     DishService dishService,
     SyncService syncService,
     RestaurantWebSocketService webSocketService)
    {
        _dishService = dishService;
        _syncService = syncService;
        _webSocketService = webSocketService;

        _webSocketService.EventReceived += OnWebSocketEventReceived;
    }

    [RelayCommand]
    public async Task LoadDishesAsync()
    {
        var menu =
            await _dishService.GetFullRestaurantMenuAsync(
                SessionService.JwtToken);

        Dishes.Clear();

        foreach (var dish in menu)
        {
            Dishes.Add(new DishManagerItem
            {
                Token = dish.Token,
                Name = dish.Name,
                IsAvailable = dish.Active
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

        await _syncService.SyncDishesAsync(SessionService.JwtToken);

        await LoadDishesAsync();

        MessageBox.Show("Dish availability changed and local DB synchronized.");
    }
    private void OnWebSocketEventReceived(WebSocketEvent e)
    {
        if (e.EntityType != "DISH" &&
            e.EntityType != "INGREDIENT" &&
            e.EntityType != "CATEGORY")
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


