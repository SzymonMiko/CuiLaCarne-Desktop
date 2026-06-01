using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Models.Base;
using QuiLaCarne.Models.DTOS;
using QuiLaCarne.Models.Lookup;
using QuiLaCarne.Models.Responses;

namespace QuiLaCarne.Services.Api;

public class SyncService : BaseApiService
{
    private const string RolesModule = "roles";
    private const string UsersModule = "users";
    private const string TablesModule = "tables";
    private const string OrdersModule = "orders";
    private const string ReservationsModule = "reservations";
    private const string ReportsModule = "reports";
    private const string OrderItemsModule = "order-items";
    private const string IngredientsModule = "ingredients";
    private const string BansModule = "bans";
    private const string AllergensModule = "allergens";
    private const string DishCategoriesModule = "dish-categories";
    private const string BanStatusesModule = "ban-statuses";
    private const string ReportStatusesModule = "report-statuses";
    private const string OrderStatusesModule = "order-statuses";
    private const string OrderItemStatusesModule = "order-item-statuses";
    private const string ReservationStatusesModule = "reservation-statuses";
    private const string TableStatusesModule = "table-statuses";
    private const string DishesModule = "dishes";

    private readonly QuiLaCarneDbContext _db;
    private readonly Dictionary<string, HashSet<string>> _lastSyncedTokens = [];

    public SyncService(HttpClient httpClient, QuiLaCarneDbContext db)
        : base(httpClient)
    {
        _db = db;
    }

    public async Task SyncRolesAsync(string jwt)
    {
        SetBearerToken(jwt);
        var syncedTokens = StartTokenCapture(RolesModule);

        var response = await HttpClient.GetAsync("api/sync/roles");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Sync roles failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var body = await response.Content.ReadAsStringAsync();

        var roles = ReadList<SyncRoleResponse>(body);

        if (roles.Count == 0)
            return;

        foreach (var dto in roles)
        {
            syncedTokens.Add(dto.Token);

            var existing = await _db.Roles.FirstOrDefaultAsync(x => x.Token == dto.Token);

            if (existing == null)
            {
                existing = new Roles
                {
                    Token = dto.Token,
                    Name = dto.Name,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };

                _db.Roles.Add(existing);
            }
            else
            {
                existing.Name = dto.Name;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task SyncUsersAsync(string jwt)
    {
        try
        {
            SetBearerToken(jwt);
            var syncedTokens = StartTokenCapture(UsersModule);

            int page = 1;
            bool hasNextPage;

            do
            {
                var result = await GetPagedAsync<SyncUserResponse>(
                    $"api/sync/users?page={page}",
                    "Sync users"
                );

                foreach (var dto in result.Items)
                {
                    syncedTokens.Add(dto.Token);

                    var existing = await _db
                        .Users.Include(x => x.Roles)
                        .FirstOrDefaultAsync(x =>
                            x.Token == dto.Token || x.Username == dto.Username
                        );

                    if (existing == null)
                    {
                        existing = new Users();

                        existing.Token = dto.Token;

                        existing.Username = dto.Username;

                        existing.Email = dto.Email;

                        existing.IsEnabled = dto.IsActive ?? false;

                        existing.PasswordHash = "";

                        existing.CreatedAt = dto.CreatedAt;

                        existing.UpdatedAt = dto.UpdatedAt;

                        _db.Users.Add(existing);
                    }
                    else if (dto.UpdatedAt > existing.UpdatedAt)
                    {
                        existing.Username = dto.Username;

                        existing.Email = dto.Email;

                        existing.IsEnabled = dto.IsActive ?? false;

                        existing.UpdatedAt = dto.UpdatedAt;
                    }

                    existing.Roles.Clear();

                    var roles = await _db
                        .Roles.Where(x => dto.RoleTokens.Contains(x.Token))
                        .ToListAsync();

                    foreach (var role in roles)
                    {
                        existing.Roles.Add(role);
                    }
                }

                await _db.SaveChangesAsync();

                hasNextPage = result.HasNextPage;
                page++;
            } while (hasNextPage);
        }
        catch (Exception ex)
        {
            throw new Exception($"SyncUsersAsync failed:\n{ex.Message}", ex);
        }
    }

    public async Task SyncTablesAsync(string jwt)
    {
        SetBearerToken(jwt);
        var syncedTokens = StartTokenCapture(TablesModule);

        int page = 1;
        bool hasNextPage;

        do
        {
            var result = await GetPagedAsync<SyncTableResponse>(
                $"api/sync/tables?page={page}",
                "Sync tables"
            );

            foreach (var dto in result.Items)
            {
                syncedTokens.Add(dto.Token);

                var existing = await _db
                    .RestaurantTables.Include(x => x.TableStatus)
                    .FirstOrDefaultAsync(x => x.Token == dto.Token);

                if (existing == null)
                {
                    existing = new RestaurantTables();

                    _db.RestaurantTables.Add(existing);
                }

                if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    existing.Token = dto.Token;

                    existing.TableNumber = dto.TableNumber;

                    existing.Capacity = dto.Capacity;

                    existing.CreatedAt = dto.CreatedAt;

                    existing.UpdatedAt = dto.UpdatedAt;
                }

                existing.TableStatus.Clear();

                var statuses = await _db
                    .TableStatuses.Where(x => dto.StatusTokens.Contains(x.Token))
                    .ToListAsync();

                foreach (var status in statuses)
                {
                    existing.TableStatus.Add(status);
                }
            }

            await _db.SaveChangesAsync();

            hasNextPage = result.HasNextPage;
            page++;
        } while (hasNextPage);
    }

    public async Task SyncOrdersAsync(string jwt)
    {
        try
        {
            await SyncOrdersCoreAsync(jwt);
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();
            await SyncOrdersCoreAsync(jwt);
        }
    }

    private async Task SyncOrdersCoreAsync(string jwt)
    {
        SetBearerToken(jwt);
        _db.ChangeTracker.Clear();

        var syncedTokens = StartTokenCapture(OrdersModule);

        int page = 1;
        bool hasNextPage;

        do
        {
            var result = await GetPagedAsync<SyncOrderResponse>(
                $"api/sync/orders?page={page}",
                "Sync orders"
            );

            foreach (var dto in result.Items)
            {
                syncedTokens.Add(dto.Token);

                var table = await _db.RestaurantTables.FirstOrDefaultAsync(x =>
                    x.Token == dto.TableToken
                );

                var user = await _db.Users.FirstOrDefaultAsync(x => x.Token == dto.UserToken);

                if (table == null || user == null)
                    continue;

                var existing = await _db.Orders.FirstOrDefaultAsync(x => x.Token == dto.Token);

                if (existing == null)
                {
                    existing = new Orders
                    {
                        Token = dto.Token,
                        TableId = table.Id,
                        UserId = user.Id,
                        CreatedAt = dto.CreatedAt,
                        UpdatedAt = dto.UpdatedAt,
                    };

                    _db.Orders.Add(existing);
                }
                else if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    existing.TableId = table.Id;
                    existing.UserId = user.Id;
                    existing.UpdatedAt = dto.UpdatedAt;
                }
            }

            await _db.SaveChangesAsync();

            hasNextPage = result.HasNextPage;
            page++;
        } while (hasNextPage);
    }

    public async Task SyncReservationsAsync(string jwt)
    {
        SetBearerToken(jwt);
        var syncedTokens = StartTokenCapture(ReservationsModule);

        int page = 1;
        bool hasNextPage;

        do
        {
            var result = await GetPagedAsync<SyncReservationResponse>(
                $"api/sync/reservations?page={page}",
                "Sync reservations"
            );

            foreach (var dto in result.Items)
            {
                syncedTokens.Add(dto.Token);

                var table = await _db.RestaurantTables.FirstOrDefaultAsync(x =>
                    x.Token == dto.TableToken
                );

                var user = await _db.Users.FirstOrDefaultAsync(x => x.Token == dto.UserToken);

                if (table == null || user == null)
                    continue;

                var existing = await _db
                    .Reservations.Include(x => x.Statuses)
                    .FirstOrDefaultAsync(x => x.Token == dto.Token);

                if (existing == null)
                {
                    existing = new Reservations
                    {
                        Token = dto.Token,
                        TableId = table.Id,
                        UserId = user.Id,
                        ReservedFrom = dto.ReservedFrom,
                        ReservedUntil = dto.ReservedUntil,
                        CreatedAt = dto.CreatedAt,
                        UpdatedAt = dto.UpdatedAt,
                    };

                    _db.Reservations.Add(existing);
                }
                else if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    existing.TableId = table.Id;
                    existing.UserId = user.Id;
                    existing.ReservedFrom = dto.ReservedFrom;
                    existing.ReservedUntil = dto.ReservedUntil;
                    existing.UpdatedAt = dto.UpdatedAt;
                }

                existing.Statuses.Clear();

                var statuses = await _db
                    .ReservationStatuses.Where(x => dto.StatusTokens.Contains(x.Token))
                    .ToListAsync();

                foreach (var status in statuses)
                {
                    existing.Statuses.Add(status);
                }
            }

            await _db.SaveChangesAsync();

            hasNextPage = result.HasNextPage;
            page++;
        } while (hasNextPage);
    }

    public async Task SyncReportsAsync(string jwt)
    {
        SetBearerToken(jwt);
        var syncedTokens = StartTokenCapture(ReportsModule);

        int page = 1;
        bool hasNextPage;

        do
        {
            var response = await HttpClient.GetAsync($"api/sync/reports?page={page}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new Exception(
                    $"Sync reports failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
                );
            }

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponse<PagedResult<SyncGuestReportResponse>>
            >();

            if (result?.Data?.Items == null)
                throw new Exception("Sync reports failed: response did not contain report items.");

            foreach (var dto in result.Data.Items)
            {
                syncedTokens.Add(dto.Token);

                var reportedUser = await _db.Users.FirstOrDefaultAsync(x =>
                    x.Token == dto.GuestToken
                );

                var reporter = await _db.Users.FirstOrDefaultAsync(x =>
                    x.Token == dto.ReporterToken
                );

                if (reportedUser == null || reporter == null)
                    continue;

                var existing = await _db
                    .GuestReports.Include(x => x.Statuses)
                    .FirstOrDefaultAsync(x => x.Token == dto.Token);

                if (existing == null)
                {
                    existing = new GuestReports
                    {
                        Token = dto.Token,
                        ReporterId = reporter.Id,
                        ReportedUserId = reportedUser.Id,
                        Description = dto.Reason,
                        CreatedAt = dto.CreatedAt,
                        UpdatedAt = dto.UpdatedAt,
                    };

                    _db.GuestReports.Add(existing);
                }
                else if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    existing.ReporterId = reporter.Id;
                    existing.ReportedUserId = reportedUser.Id;
                    existing.Description = dto.Reason;
                    existing.UpdatedAt = dto.UpdatedAt;
                }

                existing.Statuses.Clear();

                var statuses = await _db
                    .GuestReportStatuses.Where(x => dto.StatusTokens.Contains(x.Token))
                    .ToListAsync();

                foreach (var status in statuses)
                {
                    existing.Statuses.Add(status);
                }
            }

            await _db.SaveChangesAsync();

            hasNextPage = result.Data.HasNextPage;
            page++;
        } while (hasNextPage);
    }

    public async Task SyncOrderItemsAsync(string jwt)
    {
        SetBearerToken(jwt);
        var syncedTokens = StartTokenCapture(OrderItemsModule);

        int page = 1;
        bool hasNextPage;

        do
        {
            var result = await GetPagedAsync<SyncOrderItemsResponse>(
                $"api/sync/order-items?page={page}",
                "Sync order items"
            );

            foreach (var dto in result.Items)
            {
                syncedTokens.Add(dto.Token);

                var order = await _db.Orders.FirstOrDefaultAsync(x => x.Token == dto.OrderToken);

                var dish = await _db.Dishes.FirstOrDefaultAsync(x => x.Token == dto.DishToken);

                if (order == null || dish == null)
                    continue;

                var existing = await _db.OrderItems.FirstOrDefaultAsync(x => x.Token == dto.Token);

                if (existing == null)
                {
                    existing = new OrderItems
                    {
                        Token = dto.Token,
                        OrderId = order.Id,
                        DishId = dish.Id,
                        Quantity = dto.Quantity,
                        Note = dto.Note,
                        CreatedAt = dto.CreatedAt,
                        UpdatedAt = dto.UpdatedAt,
                    };

                    _db.OrderItems.Add(existing);
                }
                else if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    existing.OrderId = order.Id;
                    existing.DishId = dish.Id;
                    existing.Quantity = dto.Quantity;
                    existing.Note = dto.Note;
                    existing.UpdatedAt = dto.UpdatedAt;
                }
            }

            await _db.SaveChangesAsync();

            hasNextPage = result.HasNextPage;
            page++;
        } while (hasNextPage);
    }

    public async Task SyncIngredientsAsync(string jwt)
    {
        SetBearerToken(jwt);
        var syncedTokens = StartTokenCapture(IngredientsModule);

        int page = 1;
        bool hasNextPage;

        do
        {
            var result = await GetPagedAsync<SyncIngredientResponse>(
                $"api/sync/ingredients?page={page}",
                "Sync ingredients"
            );

            foreach (var dto in result.Items)
            {
                syncedTokens.Add(dto.Token);

                var name = dto.DisplayName;

                var existing = await _db.Ingredients.FirstOrDefaultAsync(x => x.Token == dto.Token);

                if (existing == null)
                {
                    existing = new Ingredients
                    {
                        Token = dto.Token,
                        Name = name,
                        CreatedAt = dto.CreatedAt,
                        UpdatedAt = dto.UpdatedAt,
                    };

                    _db.Ingredients.Add(existing);
                }
                else if (dto.UpdatedAt > existing.UpdatedAt || existing.Name != name)
                {
                    existing.Name = name;
                    existing.UpdatedAt =
                        dto.UpdatedAt > existing.UpdatedAt ? dto.UpdatedAt : existing.UpdatedAt;
                }
            }

            await _db.SaveChangesAsync();

            hasNextPage = result.HasNextPage;
            page++;
        } while (hasNextPage);
    }

    public async Task SyncBansAsync(string jwt)
    {
        SetBearerToken(jwt);
        var syncedTokens = StartTokenCapture(BansModule);

        int page = 1;
        bool hasNextPage;

        do
        {
            var response = await HttpClient.GetAsync($"api/sync/bans?page={page}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new Exception(
                    $"Sync bans failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
                );
            }

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponse<PagedResult<SyncBanResponse>>
            >();

            if (result?.Data?.Items == null)
                throw new Exception("Sync bans failed: response did not contain ban items.");

            foreach (var dto in result.Data.Items)
            {
                syncedTokens.Add(dto.Token);

                var user = await _db.Users.FirstOrDefaultAsync(x => x.Token == dto.UserToken);

                if (user == null)
                    continue;

                var existing = await _db
                    .Bans.Include(x => x.Statuses)
                    .FirstOrDefaultAsync(x => x.Token == dto.Token);

                if (existing == null)
                {
                    existing = new Bans
                    {
                        Token = dto.Token,
                        UserId = user.Id,
                        Reason = dto.Reason,
                        ExpiresAt = dto.ExpiresAt,
                        IsPermanent = dto.IsPermanent,
                        CreatedAt = dto.CreatedAt,
                        UpdatedAt = dto.UpdatedAt,
                    };

                    _db.Bans.Add(existing);
                }
                else if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    existing.UserId = user.Id;
                    existing.Reason = dto.Reason;
                    existing.ExpiresAt = dto.ExpiresAt;
                    existing.IsPermanent = dto.IsPermanent;
                    existing.UpdatedAt = dto.UpdatedAt;
                }

                existing.Statuses.Clear();

                var statuses = await _db
                    .BanStatuses.Where(x => dto.StatusTokens.Contains(x.Token))
                    .ToListAsync();

                foreach (var status in statuses)
                {
                    existing.Statuses.Add(status);
                }
            }

            await _db.SaveChangesAsync();

            hasNextPage = result.Data.HasNextPage;

            page++;
        } while (hasNextPage);
    }

    public async Task<SyncBootstrapResponse?> DownloadBootstrapManifestAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response = await HttpClient.GetAsync("api/sync/bootstrap");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Download sync bootstrap failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        return await response.Content.ReadFromJsonAsync<SyncBootstrapResponse>();
    }

    public async Task SyncDictionariesAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response = await HttpClient.GetAsync("api/sync/dictionaries");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Sync dictionaries failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var body = await response.Content.ReadAsStringAsync();

        var dictionaries = ReadData<SyncDictionariesResponse>(body);

        if (dictionaries == null)
            throw new Exception("Sync dictionaries failed: response did not contain dictionaries.");

        CaptureTokens(AllergensModule, dictionaries.Allergens);
        CaptureTokens(DishCategoriesModule, dictionaries.DishCategories);
        CaptureTokens(BanStatusesModule, dictionaries.BanStatuses);
        CaptureTokens(ReportStatusesModule, dictionaries.ReportStatuses);
        CaptureTokens(OrderStatusesModule, dictionaries.OrderStatuses);
        CaptureTokens(OrderItemStatusesModule, dictionaries.OrderItemStatuses);
        CaptureTokens(ReservationStatusesModule, dictionaries.ReservationStatuses);
        CaptureTokens(TableStatusesModule, dictionaries.TableStatuses);

        await UpsertDictionaryAsync(_db.Allergens, dictionaries.Allergens);
        await UpsertDictionaryAsync(_db.DishesCategories, dictionaries.DishCategories);
        await UpsertDictionaryAsync(_db.BanStatuses, dictionaries.BanStatuses);
        await UpsertDictionaryAsync(_db.GuestReportStatuses, dictionaries.ReportStatuses);
        await UpsertDictionaryAsync(_db.OrderStatuses, dictionaries.OrderStatuses);
        await UpsertDictionaryAsync(_db.OrderItemsStatuses, dictionaries.OrderItemStatuses);
        await UpsertDictionaryAsync(_db.ReservationStatuses, dictionaries.ReservationStatuses);
        await UpsertDictionaryAsync(_db.TableStatuses, dictionaries.TableStatuses);

        await _db.SaveChangesAsync();
    }

    public async Task SyncDishesAsync(string jwt)
    {
        SetBearerToken(jwt);
        var syncedTokens = StartTokenCapture(DishesModule);

        int page = 1;
        bool hasNextPage;

        do
        {
            var response = await HttpClient.GetAsync($"api/sync/dishes?page={page}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new Exception(
                    $"Sync dishes failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
                );
            }

            var result = await response.Content.ReadFromJsonAsync<
                ApiResponse<PagedResult<SyncDishResponse>>
            >();

            if (result?.Data?.Items == null)
                throw new Exception("Sync dishes failed: response did not contain dish items.");

            foreach (var dto in result.Data.Items)
            {
                syncedTokens.Add(dto.Token);

                var category = await _db.DishesCategories.FirstOrDefaultAsync(x =>
                    x.Token == dto.CategoryToken
                );

                if (category == null)
                    continue;

                var existing = await _db
                    .Dishes.Include(x => x.Ingredients)
                    .FirstOrDefaultAsync(x => x.Token == dto.Token);

                if (existing == null)
                {
                    existing = new Dishes
                    {
                        Token = dto.Token,
                        Name = dto.Name,
                        Description = dto.Description,
                        Price = dto.Price,
                        AvailableFrom = dto.AvailableFrom,
                        CategoryId = category.Id,
                        CreatedAt = dto.CreatedAt,
                        UpdatedAt = dto.UpdatedAt,
                    };

                    _db.Dishes.Add(existing);
                }
                else if (
                    dto.UpdatedAt > existing.UpdatedAt
                    || existing.Name != dto.Name
                    || existing.Description != dto.Description
                    || existing.Price != dto.Price
                    || existing.AvailableFrom != dto.AvailableFrom
                    || existing.CategoryId != category.Id
                )
                {
                    existing.Name = dto.Name;
                    existing.Description = dto.Description;
                    existing.Price = dto.Price;
                    existing.AvailableFrom = dto.AvailableFrom;
                    existing.CategoryId = category.Id;
                    existing.UpdatedAt =
                        dto.UpdatedAt > existing.UpdatedAt ? dto.UpdatedAt : existing.UpdatedAt;
                }

                existing.Ingredients.Clear();

                var ingredients = await _db
                    .Ingredients.Where(x => dto.IngredientTokens.Contains(x.Token))
                    .ToListAsync();

                foreach (var ingredient in ingredients)
                {
                    existing.Ingredients.Add(ingredient);
                }
            }

            await _db.SaveChangesAsync();

            hasNextPage = result.Data.HasNextPage;
            page++;
        } while (hasNextPage);
    }

    public async Task SyncWholeDatabaseAsync(string jwt)
    {
        _lastSyncedTokens.Clear();

        await SyncDictionariesAsync(jwt);
        await SyncRolesAsync(jwt);

        await SyncUsersAsync(jwt);
        await SyncTablesAsync(jwt);

        await SyncIngredientsAsync(jwt);
        await SyncDishesAsync(jwt);

        await SyncReservationsAsync(jwt);
        await SyncOrdersAsync(jwt);
        await SyncOrderItemsAsync(jwt);

        await SyncBansAsync(jwt);
        await SyncReportsAsync(jwt);

        await DeleteRowsMissingFromServerAsync();
    }

    private HashSet<string> StartTokenCapture(string module)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _lastSyncedTokens[module] = tokens;
        return tokens;
    }

    private void CaptureTokens(string module, IEnumerable<SyncDictionaryItemResponse> items)
    {
        _lastSyncedTokens[module] = items
            .Select(x => x.Token)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task DeleteRowsMissingFromServerAsync()
    {
        await DeleteMissingLocalRowsAsync(_db.OrderItems, OrderItemsModule);
        await DeleteMissingLocalRowsAsync(_db.GuestReports, ReportsModule);
        await DeleteMissingLocalRowsAsync(_db.Bans, BansModule);
        await DeleteMissingLocalRowsAsync(_db.Reservations, ReservationsModule);
        await DeleteMissingLocalRowsAsync(_db.Orders, OrdersModule);
        await DeleteMissingLocalRowsAsync(_db.Dishes, DishesModule);
        await DeleteMissingLocalRowsAsync(_db.Ingredients, IngredientsModule);
        await DeleteMissingLocalRowsAsync(_db.RestaurantTables, TablesModule);
        await DeleteMissingLocalRowsAsync(_db.Users, UsersModule);
        await DeleteMissingLocalRowsAsync(_db.Roles, RolesModule);

        await DeleteMissingLocalRowsAsync(_db.Allergens, AllergensModule);
        await DeleteMissingLocalRowsAsync(_db.DishesCategories, DishCategoriesModule);
        await DeleteMissingLocalRowsAsync(_db.BanStatuses, BanStatusesModule);
        await DeleteMissingLocalRowsAsync(_db.GuestReportStatuses, ReportStatusesModule);
        await DeleteMissingLocalRowsAsync(_db.OrderItemsStatuses, OrderItemStatusesModule);
        await DeleteMissingLocalRowsAsync(_db.OrderStatuses, OrderStatusesModule);
        await DeleteMissingLocalRowsAsync(_db.ReservationStatuses, ReservationStatusesModule);
        await DeleteMissingLocalRowsAsync(_db.TableStatuses, TableStatusesModule);
    }

    private async Task DeleteMissingLocalRowsAsync<T>(DbSet<T> set, string module)
        where T : BaseEntity
    {
        if (!_lastSyncedTokens.TryGetValue(module, out var remoteTokens))
            return;

        var localRowsToDelete = await set
            .Where(local => !remoteTokens.Contains(local.Token))
            .ToListAsync();

        if (localRowsToDelete.Count == 0)
            return;

        set.RemoveRange(localRowsToDelete);
        await _db.SaveChangesAsync();
    }

    private async Task UpsertDictionaryAsync<T>(
        DbSet<T> set,
        IEnumerable<SyncDictionaryItemResponse> items
    )
        where T : BaseNamedEntity, new()
    {
        foreach (var dto in items)
        {
            var existing = await set.FirstOrDefaultAsync(x => x.Token == dto.Token);

            var name = string.IsNullOrWhiteSpace(dto.NamePl) ? dto.NameEn : dto.NamePl;

            if (existing == null)
            {
                existing = new T
                {
                    Token = dto.Token,
                    Name = name,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };

                set.Add(existing);
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                existing.Name = name;
            }
        }
    }

    private async Task<PagedResult<T>> GetPagedAsync<T>(string url, string operationName)
    {
        var response = await HttpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"{operationName} failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<T>>>();

        return result?.Data
            ?? throw new Exception($"{operationName} failed: response did not contain paged data.");
    }

    private static T? ReadData<T>(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var apiResponse = TryDeserialize<ApiResponse<T>>(json, options);
        if (apiResponse is not null && apiResponse.Data is not null)
            return apiResponse.Data;

        return TryDeserialize<T>(json, options);
    }

    private static List<T> ReadList<T>(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var paged = TryDeserialize<ApiResponse<PagedResult<T>>>(json, options);
        if (paged?.Data?.Items != null)
            return paged.Data.Items;

        var listResponse = TryDeserialize<ApiResponse<List<T>>>(json, options);
        if (listResponse?.Data != null)
            return listResponse.Data;

        var list = TryDeserialize<List<T>>(json, options);
        if (list != null)
            return list;

        throw new JsonException("Unexpected sync list response.");
    }

    private static T? TryDeserialize<T>(string json, JsonSerializerOptions options)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
