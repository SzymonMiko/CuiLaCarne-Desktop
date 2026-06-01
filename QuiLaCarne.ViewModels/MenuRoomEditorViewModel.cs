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
    private readonly LookupService _lookupService;
    private readonly ReservationService _reservationService;
    private readonly SyncService _syncService;
    private readonly IRealtimeUpdateService _realtimeUpdateService;
    private readonly HashSet<string> _hiddenDishTokens = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<MenuDishRow> Dishes { get; } = new();

    public ObservableCollection<DishCategoryOption> DishCategories { get; } = new();

    public ObservableCollection<IngredientSelectionRow> IngredientsForDish { get; } = new();

    public ObservableCollection<TableMapRow> Tables { get; } = new();

    public ObservableCollection<TableOrderRow> SelectedTableOrders { get; } = new();

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

    private string newIngredientNamePl = "";
    public string NewIngredientNamePl
    {
        get => newIngredientNamePl;
        set => SetProperty(ref newIngredientNamePl, value);
    }

    private string newIngredientNameEn = "";
    public string NewIngredientNameEn
    {
        get => newIngredientNameEn;
        set => SetProperty(ref newIngredientNameEn, value);
    }

    private string newDishName = "";
    public string NewDishName
    {
        get => newDishName;
        set => SetProperty(ref newDishName, value);
    }

    private decimal newDishPrice;
    public decimal NewDishPrice
    {
        get => newDishPrice;
        set => SetProperty(ref newDishPrice, value);
    }

    private DishCategoryOption? selectedDishCategory;
    public DishCategoryOption? SelectedDishCategory
    {
        get => selectedDishCategory;
        set => SetProperty(ref selectedDishCategory, value);
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

    private TableMapRow? selectedTable;
    public TableMapRow? SelectedTable
    {
        get => selectedTable;
        set
        {
            if (SetProperty(ref selectedTable, value))
            {
                foreach (var table in Tables)
                {
                    table.IsSelected = table.Token == value?.Token;
                }

                _ = LoadSelectedTableOrdersAsync();
            }
        }
    }

    private string selectedTableOrdersText = "Select a table to see orders.";
    public string SelectedTableOrdersText
    {
        get => selectedTableOrdersText;
        set => SetProperty(ref selectedTableOrdersText, value);
    }

    public MenuRoomEditorViewModel(
        QuiLaCarneDbContext db,
        DishService dishService,
        LookupService lookupService,
        ReservationService reservationService,
        SyncService syncService,
        IRealtimeUpdateService realtimeUpdateService)
    {
        _db = db;
        _dishService = dishService;
        _lookupService = lookupService;
        _reservationService = reservationService;
        _syncService = syncService;
        _realtimeUpdateService = realtimeUpdateService;
        _realtimeUpdateService.LocalDataChanged += OnRealtimeDataChanged;

        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var selectedCategoryToken = SelectedDishCategory?.Token;
        var selectedTableToken = SelectedTable?.Token;
        var selectedIngredientTokens = IngredientsForDish
            .Where(i => i.IsSelected)
            .Select(i => i.Token)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dishes = await _db.Dishes
            .AsNoTracking()
            .Where(d => !_hiddenDishTokens.Contains(d.Token))
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

        var categories = await _db.DishesCategories
            .AsNoTracking()
            .Where(c => !c.Name.StartsWith("DELETED_") && !c.Token.StartsWith("DELETED_"))
            .OrderBy(c => c.Name)
            .ToListAsync();

        DishCategories.Clear();

        foreach (var category in categories)
        {
            DishCategories.Add(new DishCategoryOption
            {
                Token = category.Token,
                Name = category.Name
            });
        }

        SelectedDishCategory =
            DishCategories.FirstOrDefault(c => c.Token == selectedCategoryToken) ??
            DishCategories.FirstOrDefault();

        var ingredients = await _db.Ingredients
            .AsNoTracking()
            .Where(i => !i.Name.StartsWith("DELETED_") && !i.Token.StartsWith("DELETED_"))
            .OrderBy(i => i.Name)
            .ToListAsync();

        IngredientsForDish.Clear();

        foreach (var ingredient in ingredients)
        {
            IngredientsForDish.Add(new IngredientSelectionRow
            {
                Token = ingredient.Token,
                Name = ingredient.DisplayName,
                IsSelected = selectedIngredientTokens.Contains(ingredient.Token)
            });
        }

        var tables = await _db.RestaurantTables
            .AsNoTracking()
            .Include(t => t.TableStatus)
            .OrderBy(t => t.TableNumber)
            .ToListAsync();

        Tables.Clear();

        foreach (var t in tables)
        {
            Tables.Add(new TableMapRow
            {
                Token = t.Token,
                Id = t.Id,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                StatusText = t.TableStatus.Count == 0
                    ? "No status"
                    : string.Join(", ", t.TableStatus.OrderBy(s => s.Name).Select(s => s.Name)),
                IsSelected = t.Token == selectedTableToken
            });
        }

        SelectedTable =
            Tables.FirstOrDefault(t => t.Token == selectedTableToken) ??
            SelectedTable;

        if (SelectedTable != null && !Tables.Any(t => t.Token == SelectedTable.Token))
        {
            SelectedTable = null;
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
    private async Task MakeDishAvailableAsync()
    {
        if (SelectedDish == null)
        {
            return;
        }

        await _dishService.ChangeDishAvailabilityAsync(
            SessionService.JwtToken,
            SelectedDish.Token,
            true,
            null);

        MessageBox.Show("Dish available request sent. The editor will refresh after the server confirms it.");
    }

    [RelayCommand]
    private async Task AddIngredientAsync()
    {
        if (string.IsNullOrWhiteSpace(NewIngredientNamePl))
        {
            MessageBox.Show("Ingredient name is required.");
            return;
        }

        var namePl = NewIngredientNamePl.Trim();
        var nameEn = string.IsNullOrWhiteSpace(NewIngredientNameEn)
            ? namePl
            : NewIngredientNameEn.Trim();

        var added = await _lookupService.AddIngredientAsync(
            SessionService.JwtToken,
            namePl,
            nameEn,
            []);

        if (!added)
        {
            MessageBox.Show("Ingredient was not added.");
            return;
        }

        NewIngredientNamePl = "";
        NewIngredientNameEn = "";

        await _syncService.SyncIngredientsAsync(SessionService.JwtToken);
        await LoadAsync();

        MessageBox.Show("Ingredient added.");
    }

    [RelayCommand]
    private async Task AddDishAsync()
    {
        if (string.IsNullOrWhiteSpace(NewDishName))
        {
            MessageBox.Show("Dish name is required.");
            return;
        }

        if (NewDishPrice <= 0)
        {
            MessageBox.Show("Dish price must be greater than 0.");
            return;
        }

        if (SelectedDishCategory == null)
        {
            MessageBox.Show("Dish category is required.");
            return;
        }

        var ingredientTokens = IngredientsForDish
            .Where(i => i.IsSelected)
            .Select(i => i.Token)
            .ToList();

        if (ingredientTokens.Count == 0)
        {
            MessageBox.Show("Choose at least one ingredient.");
            return;
        }

        await _dishService.AddDishAsync(
            SessionService.JwtToken,
            NewDishName.Trim(),
            (int)NewDishPrice,
            SelectedDishCategory.Token,
            ingredientTokens,
            null);

        NewDishName = "";
        NewDishPrice = 0;

        foreach (var ingredient in IngredientsForDish)
        {
            ingredient.IsSelected = false;
        }

        await _syncService.SyncDishesAsync(SessionService.JwtToken);
        await LoadAsync();

        MessageBox.Show("Dish added.");
    }

    [RelayCommand]
    private async Task DeleteDishAsync()
    {
        if (SelectedDish == null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Delete {SelectedDish.Name}?",
            "Delete dish",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var deleted = await _dishService.DeleteDishAsync(
            SessionService.JwtToken,
            SelectedDish.Token);

        if (!deleted)
        {
            MessageBox.Show("Dish was not deleted.");
            return;
        }

        _hiddenDishTokens.Add(SelectedDish.Token);
        Dishes.Remove(SelectedDish);
        SelectedDish = null;
        EditedPrice = 0;
        BlockReason = "";

        MessageBox.Show("Dish delete request sent.");
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

    [RelayCommand]
    private async Task SelectTableAsync(TableMapRow table)
    {
        SelectedTable = table;

        await _syncService.SyncUsersAsync(SessionService.JwtToken);
        await _syncService.SyncTablesAsync(SessionService.JwtToken);
        await _syncService.SyncOrdersAsync(SessionService.JwtToken);
        await _syncService.SyncOrderItemsAsync(SessionService.JwtToken);
        await LoadSelectedTableOrdersAsync();
    }

    private async Task LoadSelectedTableOrdersAsync()
    {
        SelectedTableOrders.Clear();

        if (SelectedTable == null)
        {
            SelectedTableOrdersText = "Select a table to see orders.";
            return;
        }

        var table = SelectedTable;

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Table)
            .Include(o => o.User)
            .Include(o => o.Statuses)
            .Include(o => o.Items)
                .ThenInclude(i => i.Dish)
            .Include(o => o.Items)
                .ThenInclude(i => i.Statuses)
            .ToListAsync();

        var tableOrders = orders
            .Where(o => o.TableId == table.Id || (o.Table != null && o.Table.Token == table.Token))
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        SelectedTableOrdersText = tableOrders.Count == 0
            ? "No orders found for this table."
            : $"{tableOrders.Count} order(s) on this table.";

        foreach (var order in tableOrders)
        {
            var dishes = order.Items.Count == 0
                ? "No dishes"
                : string.Join(", ", order.Items
                    .OrderBy(i => i.Dish?.Name ?? "")
                    .Select(i => $"{i.Quantity}x {i.Dish?.Name ?? "Dish"}"));

            SelectedTableOrders.Add(new TableOrderRow
            {
                Token = order.Token,
                CreatedAt = order.CreatedAt.LocalDateTime,
                Guest = order.User.Username,
                StatusText = order.Statuses.Count == 0
                    ? "No status"
                    : string.Join(", ", order.Statuses.OrderBy(s => s.Name).Select(s => s.Name)),
                DishesText = dishes
            });
        }
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
            entityType != "TABLE_STATUS" &&
            entityType != "ORDER" &&
            entityType != "ORDER_ITEM" &&
            entityType != "ORDER_STATUS" &&
            entityType != "ORDER_ITEM_STATUS")
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

public class DishCategoryOption
{
    public string Token { get; set; } = "";

    public string Name { get; set; } = "";
}

public partial class IngredientSelectionRow : ObservableObject
{
    public string Token { get; set; } = "";

    public string Name { get; set; } = "";

    [ObservableProperty]
    private bool isSelected;
}

public partial class TableMapRow : ObservableObject
{
    public string Token { get; set; } = "";

    public Guid Id { get; set; }

    public int TableNumber { get; set; }

    public int Capacity { get; set; }

    public string StatusText { get; set; } = "";

    public string CardBackground => IsSelected ? "#EAF7ED" : "#F5F5F5";

    public string CardBorderBrush => IsSelected ? "#E53935" : "#19A93A";

    public string CardBorderThickness => IsSelected ? "2" : "1";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CardBackground))]
    [NotifyPropertyChangedFor(nameof(CardBorderBrush))]
    [NotifyPropertyChangedFor(nameof(CardBorderThickness))]
    private bool isSelected;
}

public class TableOrderRow
{
    public string Token { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public string Guest { get; set; } = "";

    public string StatusText { get; set; } = "";

    public string DishesText { get; set; } = "";

    public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd HH:mm");
}
