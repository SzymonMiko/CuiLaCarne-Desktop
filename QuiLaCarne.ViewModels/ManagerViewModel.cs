using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;
using System.Collections.ObjectModel;

public partial class ManagerViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly DishService _dishService;
    private readonly IRealtimeUpdateService _realtimeUpdateService;
    private readonly IAppDialogService _dialog;
    private readonly IUiDispatcherService _uiDispatcher;

    [ObservableProperty]
    private ObservableCollection<DishManagerItem> dishes = new();

    [ObservableProperty]
    private DishManagerItem? selectedDish;

    [ObservableProperty]
    private string unavailableReason = "Niedostępne";

    public ManagerViewModel(
        QuiLaCarneDbContext db,
        DishService dishService,
        IRealtimeUpdateService realtimeUpdateService,
        IAppDialogService dialog,
        IUiDispatcherService uiDispatcher)
    {
        _db = db;
        _dishService = dishService;
        _realtimeUpdateService = realtimeUpdateService;
        _dialog = dialog;
        _uiDispatcher = uiDispatcher;
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
            _dialog.ShowMessage("Najpierw wybierz danie.");
            return;
        }

        var newAvailable = !SelectedDish.IsAvailable;

        await _dishService.ChangeDishAvailabilityAsync(
            SessionService.JwtToken,
            SelectedDish.Token,
            newAvailable,
            newAvailable ? "" : UnavailableReason);

        _dialog.ShowMessage("Zmiana wysłana. Lista odświeży się po potwierdzeniu z serwera.");
    }

    [RelayCommand]
    public async Task BlockSelectedDishAsync()
    {
        if (SelectedDish == null)
        {
            _dialog.ShowMessage("Najpierw wybierz danie.");
            return;
        }

        await _dishService.ChangeDishAvailabilityAsync(
            SessionService.JwtToken,
            SelectedDish.Token,
            false,
            UnavailableReason);

        _dialog.ShowMessage("Prośba o blokadę wysłana. Lista odświeży się po potwierdzeniu z serwera.");
    }

    [RelayCommand]
    public async Task MakeSelectedDishAvailableAsync()
    {
        if (SelectedDish == null)
        {
            _dialog.ShowMessage("Najpierw wybierz danie.");
            return;
        }

        await _dishService.ChangeDishAvailabilityAsync(
            SessionService.JwtToken,
            SelectedDish.Token,
            true,
            null);

        _dialog.ShowMessage("Prośba o przywrócenie dostępności wysłana. Lista odświeży się po potwierdzeniu z serwera.");
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

        _ = _uiDispatcher.InvokeAsync(LoadDishesAsync);
    }
}

public partial class DishManagerItem : ObservableObject
{
    public string Token { get; set; } = "";

    public string Name { get; set; } = "";

    [ObservableProperty]
    private bool isAvailable;
}
