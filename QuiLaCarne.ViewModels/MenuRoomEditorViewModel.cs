using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;

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
    private readonly HashSet<string> _hiddenTableTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _hiddenLookupTokens = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<MenuDishRow> Dishes { get; } = new();

    public ObservableCollection<DishCategoryOption> DishCategories { get; } = new();

    public ObservableCollection<IngredientSelectionRow> IngredientsForDish { get; } = new();

    public ObservableCollection<AllergenSelectionRow> AllergensForIngredient { get; } = new();

    public ObservableCollection<TableMapRow> Tables { get; } = new();

    public ObservableCollection<TableOrderRow> SelectedTableOrders { get; } = new();

    public ObservableCollection<DeleteTargetTypeRow> DeleteTargetTypes { get; } =
        new(
        [
            new("Dish category", "dish-category"),
            new("Ingredient", "ingredient"),
            new("Allergen", "allergen"),
            new("Table status", "table-status"),
            new("Order status", "order-status"),
            new("Order item status", "order-item-status")
        ]);

    public ObservableCollection<DeleteLookupRow> DeleteLookupItems { get; } = new();

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

    private string selectedDishPhotoPath = "";
    public string SelectedDishPhotoPath
    {
        get => selectedDishPhotoPath;
        set
        {
            if (SetProperty(ref selectedDishPhotoPath, value))
            {
                OnPropertyChanged(nameof(SelectedDishPhotoName));
            }
        }
    }

    public string SelectedDishPhotoName =>
        string.IsNullOrWhiteSpace(SelectedDishPhotoPath)
            ? "No new photo selected"
            : Path.GetFileName(SelectedDishPhotoPath);

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

    private string newDishPhotoPath = "";
    public string NewDishPhotoPath
    {
        get => newDishPhotoPath;
        set
        {
            if (SetProperty(ref newDishPhotoPath, value))
            {
                OnPropertyChanged(nameof(NewDishPhotoName));
            }
        }
    }

    public string NewDishPhotoName =>
        string.IsNullOrWhiteSpace(NewDishPhotoPath)
            ? "No photo selected"
            : Path.GetFileName(NewDishPhotoPath);

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

    private DeleteTargetTypeRow? selectedDeleteTargetType;
    public DeleteTargetTypeRow? SelectedDeleteTargetType
    {
        get => selectedDeleteTargetType;
        set
        {
            if (SetProperty(ref selectedDeleteTargetType, value))
            {
                _ = LoadDeleteLookupItemsAsync();
            }
        }
    }

    private DeleteLookupRow? selectedDeleteLookupItem;
    public DeleteLookupRow? SelectedDeleteLookupItem
    {
        get => selectedDeleteLookupItem;
        set => SetProperty(ref selectedDeleteLookupItem, value);
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

        selectedDeleteTargetType = DeleteTargetTypes.FirstOrDefault();
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
        var selectedAllergenTokens = AllergensForIngredient
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

        var allergens = await _db.Allergens
            .AsNoTracking()
            .Where(a => !a.Name.StartsWith("DELETED_") && !a.Token.StartsWith("DELETED_"))
            .OrderBy(a => a.Name)
            .ToListAsync();

        AllergensForIngredient.Clear();

        foreach (var allergen in allergens)
        {
            AllergensForIngredient.Add(new AllergenSelectionRow
            {
                Token = allergen.Token,
                Name = allergen.Name,
                IsSelected = selectedAllergenTokens.Contains(allergen.Token)
            });
        }

        var tables = await _db.RestaurantTables
            .AsNoTracking()
            .Include(t => t.TableStatus)
            .Where(t => !_hiddenTableTokens.Contains(t.Token))
            .OrderBy(t => t.TableNumber)
            .ToListAsync();

        Tables.Clear();

        foreach (var t in tables)
        {
            var statusNames = t.TableStatus
                .Select(s => s.Name)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .OrderBy(s => s)
                .ToList();
            var statusCode = GetPrimaryTableStatus(statusNames);

            Tables.Add(new TableMapRow
            {
                Token = t.Token,
                Id = t.Id,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                StatusCode = statusCode,
                StatusText = statusNames.Count == 0
                    ? "No status"
                    : string.Join(", ", statusNames),
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

        await LoadDeleteLookupItemsAsync();
    }

    private static string GetPrimaryTableStatus(IReadOnlyCollection<string> statusNames)
    {
        if (statusNames.Count == 0)
        {
            return "NO_STATUS";
        }

        string[] priority =
        [
            "OUT_OF_SERVICE",
            "CLEANING",
            "OCCUPIED",
            "AVAILABLE"
        ];

        foreach (var status in priority)
        {
            if (statusNames.Any(name => NormalizeStatus(name) == status))
            {
                return status;
            }
        }

        return NormalizeStatus(statusNames.First());
    }

    private static string NormalizeStatus(string status)
    {
        return status
            .Trim()
            .Replace("ROLE_", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "_")
            .Replace("-", "_")
            .ToUpperInvariant();
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
            AllergensForIngredient.Where(a => a.IsSelected).Select(a => a.Token).ToList());

        if (!added)
        {
            MessageBox.Show("Ingredient was not added.");
            return;
        }

        NewIngredientNamePl = "";
        NewIngredientNameEn = "";
        foreach (var allergen in AllergensForIngredient)
        {
            allergen.IsSelected = false;
        }

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
            string.IsNullOrWhiteSpace(NewDishPhotoPath) ? null : NewDishPhotoPath);

        NewDishName = "";
        NewDishPrice = 0;
        NewDishPhotoPath = "";

        foreach (var ingredient in IngredientsForDish)
        {
            ingredient.IsSelected = false;
        }

        await _syncService.SyncDishesAsync(SessionService.JwtToken);
        await LoadAsync();

        MessageBox.Show("Dish added.");
    }

    [RelayCommand]
    private void SelectDishPhoto()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose dish photo",
            Filter = "Image files (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            NewDishPhotoPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void SelectSelectedDishPhoto()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose new dish photo",
            Filter = "Image files (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedDishPhotoPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private async Task ChangeSelectedDishPhotoAsync()
    {
        if (SelectedDish == null)
        {
            MessageBox.Show("Select a dish first.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedDishPhotoPath))
        {
            MessageBox.Show("Choose a photo first.");
            return;
        }

        await _dishService.EditDishAsync(
            SessionService.JwtToken,
            dishToken: SelectedDish.Token,
            photoPath: SelectedDishPhotoPath);

        SelectedDishPhotoPath = "";

        await _syncService.SyncDishesAsync(SessionService.JwtToken);
        await LoadAsync();

        MessageBox.Show("Dish photo change sent.");
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
    private async Task DeleteSelectedTableAsync()
    {
        if (SelectedTable == null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Delete table {SelectedTable.TableNumber}?",
            "Delete table",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var deleted = await _reservationService.DeleteTableAsync(
            SessionService.JwtToken,
            SelectedTable.Token);

        if (!deleted)
        {
            MessageBox.Show("Table was not deleted.");
            return;
        }

        _hiddenTableTokens.Add(SelectedTable.Token);
        Tables.Remove(SelectedTable);
        SelectedTable = null;
        SelectedTableOrders.Clear();
        SelectedTableOrdersText = "Select a table to see orders.";

        MessageBox.Show("Table deleted.");
    }

    [RelayCommand]
    private async Task DeleteLookupAsync()
    {
        if (SelectedDeleteTargetType == null || SelectedDeleteLookupItem == null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Delete {SelectedDeleteLookupItem.Name}?",
            $"Delete {SelectedDeleteTargetType.Name}",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var token = SelectedDeleteLookupItem.Token;

        switch (SelectedDeleteTargetType.Key)
        {
            case "dish-category":
                await _lookupService.DeleteDishCategoryAsync(SessionService.JwtToken, token);
                break;

            case "ingredient":
                await _lookupService.DeleteIngredientAsync(SessionService.JwtToken, token);
                break;

            case "allergen":
                await _lookupService.DeleteAllergenAsync(SessionService.JwtToken, token);
                break;

            case "table-status":
                await _lookupService.DeleteTableStatusAsync(SessionService.JwtToken, token);
                break;

            case "order-status":
                await _lookupService.DeleteOrderStatusAsync(SessionService.JwtToken, token);
                break;

            case "order-item-status":
                await _lookupService.DeleteOrderItemStatusAsync(SessionService.JwtToken, token);
                break;
        }

        _hiddenLookupTokens.Add($"{SelectedDeleteTargetType.Key}:{token}");
        await LoadDeleteLookupItemsAsync();
        await LoadAsync();

        MessageBox.Show("Delete request completed.");
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

    private async Task LoadDeleteLookupItemsAsync()
    {
        DeleteLookupItems.Clear();

        if (SelectedDeleteTargetType == null)
        {
            return;
        }

        var items = SelectedDeleteTargetType.Key switch
        {
            "dish-category" => await _db.DishesCategories
                .AsNoTracking()
                .Where(x => !x.Name.StartsWith("DELETED_") && !x.Token.StartsWith("DELETED_"))
                .OrderBy(x => x.Name)
                .Select(x => new DeleteLookupRow { Token = x.Token, Name = x.Name })
                .ToListAsync(),

            "ingredient" => await _db.Ingredients
                .AsNoTracking()
                .Where(x => !x.Name.StartsWith("DELETED_") && !x.Token.StartsWith("DELETED_"))
                .OrderBy(x => x.Name)
                .Select(x => new DeleteLookupRow { Token = x.Token, Name = x.DisplayName })
                .ToListAsync(),

            "allergen" => await _db.Allergens
                .AsNoTracking()
                .Where(x => !x.Name.StartsWith("DELETED_") && !x.Token.StartsWith("DELETED_"))
                .OrderBy(x => x.Name)
                .Select(x => new DeleteLookupRow { Token = x.Token, Name = x.Name })
                .ToListAsync(),

            "table-status" => await _db.TableStatuses
                .AsNoTracking()
                .Where(x => !x.Name.StartsWith("DELETED_") && !x.Token.StartsWith("DELETED_"))
                .OrderBy(x => x.Name)
                .Select(x => new DeleteLookupRow { Token = x.Token, Name = x.Name })
                .ToListAsync(),

            "order-status" => await _db.OrderStatuses
                .AsNoTracking()
                .Where(x => !x.Name.StartsWith("DELETED_") && !x.Token.StartsWith("DELETED_"))
                .OrderBy(x => x.Name)
                .Select(x => new DeleteLookupRow { Token = x.Token, Name = x.Name })
                .ToListAsync(),

            "order-item-status" => await _db.OrderItemsStatuses
                .AsNoTracking()
                .Where(x => !x.Name.StartsWith("DELETED_") && !x.Token.StartsWith("DELETED_"))
                .OrderBy(x => x.Name)
                .Select(x => new DeleteLookupRow { Token = x.Token, Name = x.Name })
                .ToListAsync(),

            _ => []
        };

        foreach (var item in items.Where(item =>
            !_hiddenLookupTokens.Contains($"{SelectedDeleteTargetType.Key}:{item.Token}")))
        {
            DeleteLookupItems.Add(item);
        }

        SelectedDeleteLookupItem = DeleteLookupItems.FirstOrDefault();
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

public record DeleteTargetTypeRow(string Name, string Key);

public class DeleteLookupRow
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

public partial class AllergenSelectionRow : ObservableObject
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

    public string StatusCode { get; set; } = "NO_STATUS";

    public string StatusLabel => StatusCode == "NO_STATUS"
        ? "NO STATUS"
        : StatusCode;

    public string CardBackground => StatusCode switch
    {
        "AVAILABLE" => "#EAF7ED",
        "OCCUPIED" => "#FFECEC",
        "CLEANING" => "#FFF8E1",
        "OUT_OF_SERVICE" => "#EDEDED",
        _ => "#F5F5F5"
    };

    public string CardBorderBrush => IsSelected ? "#111827" : StatusCode switch
    {
        "AVAILABLE" => "#19A93A",
        "OCCUPIED" => "#E53935",
        "CLEANING" => "#D99A00",
        "OUT_OF_SERVICE" => "#666666",
        _ => "#BDBDBD"
    };

    public string StatusBadgeBackground => StatusCode switch
    {
        "AVAILABLE" => "#19A93A",
        "OCCUPIED" => "#E53935",
        "CLEANING" => "#D99A00",
        "OUT_OF_SERVICE" => "#666666",
        _ => "#9E9E9E"
    };

    public string StatusBadgeForeground => "White";

    public string CardBorderThickness => IsSelected ? "3" : "1";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CardBackground))]
    [NotifyPropertyChangedFor(nameof(CardBorderBrush))]
    [NotifyPropertyChangedFor(nameof(CardBorderThickness))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeBackground))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeForeground))]
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
