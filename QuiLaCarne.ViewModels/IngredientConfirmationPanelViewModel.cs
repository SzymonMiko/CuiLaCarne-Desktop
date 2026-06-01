using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;

public partial class IngredientConfirmationPanelViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly LookupService _lookupService;
    private readonly IRealtimeUpdateService _realtimeUpdateService;
    private readonly IAppDialogService _dialog;
    private readonly IUiDispatcherService _uiDispatcher;

    public ObservableCollection<Ingredients> Ingredients { get; } = new();

    public ObservableCollection<AffectedDishRow> AffectedDishes { get; } = new();

    [ObservableProperty]
    private Ingredients? selectedIngredient;

    [ObservableProperty]
    private string reason = "Brak składnika w kuchni";
    
    public IngredientConfirmationPanelViewModel(
        QuiLaCarneDbContext db,
        LookupService lookupService,
        IRealtimeUpdateService realtimeUpdateService,
        IAppDialogService dialog,
        IUiDispatcherService uiDispatcher)
    {
        _db = db;
        _lookupService = lookupService;
        _realtimeUpdateService = realtimeUpdateService;
        _dialog = dialog;
        _uiDispatcher = uiDispatcher;
        _realtimeUpdateService.LocalDataChanged += OnRealtimeDataChanged;
    }

    partial void OnSelectedIngredientChanged(Ingredients? value)
    {
        _ = LoadAffectedDishesAsync();
    }

    [RelayCommand]
    public async Task LoadIngredientsAsync()
    {
        var ingredients = await _db.Ingredients
    .AsNoTracking()
    .Where(i =>
        !i.Name.StartsWith("DELETED_") &&
        !i.Token.StartsWith("DELETED_"))
    .OrderBy(i => i.Name)
    .ToListAsync();

        Ingredients.Clear();

        foreach (var ingredient in ingredients)
        {
            Ingredients.Add(ingredient);
        }
    }

    [RelayCommand]
    public async Task LoadAffectedDishesAsync()
    {
        AffectedDishes.Clear();

        if (SelectedIngredient == null)
        {
            return;
        }

        var dishes = await _db.Dishes
            .Include(d => d.Ingredients)
            .AsNoTracking()
            .Where(d => d.Ingredients.Any(i => i.Token == SelectedIngredient.Token))
            .OrderBy(d => d.Name)
            .ToListAsync();

        foreach (var dish in dishes)
        {
            AffectedDishes.Add(
                new AffectedDishRow
                {
                    Token = dish.Token,
                    Name = dish.Name,
                    IsAvailable =
                        dish.AvailableFrom == null || dish.AvailableFrom <= DateTimeOffset.UtcNow,
                });
        }
    }

    [RelayCommand]
    private async Task ConfirmMissingIngredientAsync()
    {
        if (SelectedIngredient == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Reason))
        {
            _dialog.ShowMessage("Powód jest wymagany.");
            return;
        }

        try
        {
            await _lookupService.DeleteIngredientAsync(
                SessionService.JwtToken,
                SelectedIngredient.Token);

            _dialog.ShowMessage("Zmiana składnika wysłana. Dane lokalne odświeżą się po potwierdzeniu z serwera.");
        }
        catch (Exception ex) when (ex.Message.Contains("403 Forbidden"))
        {
            _dialog.ShowMessage(
                "Backend odrzucił zmianę. To konto nie ma uprawnień do usuwania składników, więc WebSocket nie wysłał aktualizacji.");
        }
        catch (Exception ex)
        {
            _dialog.ShowMessage($"Zmiana składnika nie powiodła się:\n{ex.Message}");
        }
    }

    private void OnRealtimeDataChanged(object? sender, WebSocketEvent e)
    {
        var entityType = e.EntityType.ToUpperInvariant();

        if (entityType != "INGREDIENT" &&
            entityType != "DISH" &&
            entityType != "DISH_AVAILABILITY" &&
            entityType != "MENU_AVAILABILITY")
        {
            return;
        }

        _ = _uiDispatcher.InvokeAsync(async () =>
        {
            await LoadIngredientsAsync();
            await LoadAffectedDishesAsync();
        });
    }

    public class AffectedDishRow
    {
        public string Token { get; set; } = "";

        public string Name { get; set; } = "";

        public bool IsAvailable { get; set; }
    }
}
