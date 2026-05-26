using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using System.Collections.ObjectModel;
using System.Windows;

public partial class IngredientConfirmationPanelViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly IngredientAvailabilityService _ingredientService;
    private readonly SyncService _syncService;

    public ObservableCollection<Ingredients> Ingredients { get; } = new();
    public ObservableCollection<AffectedDishRow> AffectedDishes { get; } = new();

    [ObservableProperty] private Ingredients? selectedIngredient;
    [ObservableProperty] private string reason = "Ingredient missing in kitchen";

    public IngredientConfirmationPanelViewModel(QuiLaCarneDbContext db, IngredientAvailabilityService ingredientService, SyncService syncService)
    {
        _db = db;
        _ingredientService = ingredientService;
        _syncService = syncService;
        _ = LoadIngredientsAsync();
    }

    partial void OnSelectedIngredientChanged(Ingredients? value)
    {
        _ = LoadAffectedDishesAsync();
    }

    [RelayCommand]
    public async Task LoadIngredientsAsync()
    {
        var ingredients = await _db.Ingredients.AsNoTracking().OrderBy(i => i.Name).ToListAsync();
        Ingredients.Clear();
        foreach (var ingredient in ingredients)
            Ingredients.Add(ingredient);
    }

    [RelayCommand]
    public async Task LoadAffectedDishesAsync()
    {
        AffectedDishes.Clear();
        if (SelectedIngredient == null) return;

        var dishes = await _db.Dishes
            .Include(d => d.Ingredients)
            .AsNoTracking()
            .Where(d => d.Ingredients.Any(i => i.Token == SelectedIngredient.Token))
            .OrderBy(d => d.Name)
            .ToListAsync();

        foreach (var dish in dishes)
        {
            AffectedDishes.Add(new AffectedDishRow
            {
                Token = dish.Token,
                Name = dish.Name,
                IsAvailable = dish.AvailableFrom == null || dish.AvailableFrom <= DateTimeOffset.UtcNow
            });
        }
    }

    [RelayCommand]
    private async Task ConfirmMissingIngredientAsync()
    {
        if (SelectedIngredient == null) return;
        if (string.IsNullOrWhiteSpace(Reason))
        {
            MessageBox.Show("Reason is required.");
            return;
        }

        await _ingredientService.ConfirmMissingIngredientAsync(SessionService.JwtToken, SelectedIngredient.Token, Reason);
        await _syncService.SyncIngredientsAsync(SessionService.JwtToken);
        await _syncService.SyncDishesAsync(SessionService.JwtToken);
        await LoadAffectedDishesAsync();
    }
}

public class AffectedDishRow
{
    public string Token { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsAvailable { get; set; }
}
