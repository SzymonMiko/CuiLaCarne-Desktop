using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Models.DTOS;
using QuiLaCarne.Models.Lookup;
using QuiLaCarne.Models.Responses;
using System;
using System.Net.Http.Json;
using System.Windows;

namespace QuiLaCarne.Services.Api;

public class SyncService : BaseApiService
{
    private readonly QuiLaCarneDbContext _db;

    public SyncService(
        HttpClient httpClient,
        QuiLaCarneDbContext db)
        : base(httpClient)
    {
        _db = db;
    }

    public async Task SyncUsersAsync(string jwt)
    {
        try
        {
            SetBearerToken(jwt);

            var response =
                await HttpClient.GetAsync(
                    "api/sync/users");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody =
                    await response.Content
                        .ReadAsStringAsync();

                throw new Exception(
                    $"Sync failed.\n" +
                    $"Status: {(int)response.StatusCode}\n" +
                    $"{response.StatusCode}\n\n" +
                    errorBody);
            }

            var result =
                await response.Content
                    .ReadFromJsonAsync<
                        ApiResponse<
                            PagedResult<
                                SyncUserResponse>>>();

            if (result?.Data?.Items == null)
            {
                throw new Exception(
                    "Sync returned no users.");
            }

            foreach (var dto in result.Data.Items)
            {
                var existing =
                    await _db.Users
                        .FirstOrDefaultAsync(
                            x => x.Token == dto.Token);

                // NEW USER
                if (existing == null)
                {
                    existing = new Users();

                    existing.Token =
                        dto.Token;

                    existing.Username =
                        dto.Username;

                    existing.Email =
                        dto.Email;

                    existing.IsEnabled =
                        dto.IsActive ?? false;


                    existing.PasswordHash = "";

                    existing.CreatedAt =
                        dto.CreatedAt;

                    existing.UpdatedAt =
                        dto.UpdatedAt;

                    _db.Users.Add(existing);
                }

                else if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    existing.Username =
                        dto.Username;

                    existing.Email =
                        dto.Email;

                    existing.IsEnabled =
                        dto.IsActive ?? false;

                    existing.UpdatedAt =
                        dto.UpdatedAt;
                }
            }

            await _db.SaveChangesAsync();

        }
        catch (Exception ex)
        {
            throw new Exception(
                $"SyncUsersAsync failed:\n{ex.Message}",
                ex);
        }
    }





    public async Task SyncTablesAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.GetAsync(
                "api/sync/tables");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                ApiResponse<
                    PagedResult<
                        SyncTableResponse>>>();

        if (result?.Data?.Items == null)
            return;

        foreach (var dto in result.Data.Items)
        {
            var existing =
                await _db.RestaurantTables
                    .FirstOrDefaultAsync(
                        x => x.Token == dto.Token);

            if (existing == null)
            {
                existing = new RestaurantTables();

                _db.RestaurantTables
                    .Add(existing);
            }

            if (dto.UpdatedAt > existing.UpdatedAt)
            {
                existing.Token =
                    dto.Token;

                existing.TableNumber =
                    dto.TableNumber;

                existing.Capacity =
                    dto.Capacity;

                existing.CreatedAt =
                    dto.CreatedAt;

                existing.UpdatedAt =
                    dto.UpdatedAt;
            }
        }

        await _db.SaveChangesAsync();
    }
    public async Task SyncOrdersAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.GetAsync("api/sync/orders");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Sync orders failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                ApiResponse<
                    PagedResult<
                        SyncOrderResponse>>>();

        if (result?.Data?.Items == null)
            return;

        foreach (var dto in result.Data.Items)
        {
            var table =
                await _db.RestaurantTables
                    .FirstOrDefaultAsync(x => x.Token == dto.TableToken);

            var user =
                await _db.Users
                    .FirstOrDefaultAsync(x => x.Token == dto.UserToken);

            if (table == null || user == null)
                continue;

            var existing =
                await _db.Orders
                    .FirstOrDefaultAsync(x => x.Token == dto.Token);

            if (existing == null)
            {
                existing = new Orders
                {
                    Token = dto.Token,
                    TableId = table.Id,
                    UserId = user.Id,
                    CreatedAt = dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt
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
    }
    public async Task SyncReservationsAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.GetAsync("api/sync/reservations");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Sync reservations failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                ApiResponse<
                    PagedResult<
                        SyncReservationResponse>>>();

        if (result?.Data?.Items == null)
            return;

        foreach (var dto in result.Data.Items)
        {
            var table =
                await _db.RestaurantTables
                    .FirstOrDefaultAsync(x => x.Token == dto.TableToken);

            var user =
                await _db.Users
                    .FirstOrDefaultAsync(x => x.Token == dto.UserToken);

            if (table == null || user == null)
                continue;

            var existing =
                await _db.Reservations
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
                    UpdatedAt = dto.UpdatedAt
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
        }

        await _db.SaveChangesAsync();
    }





    public async Task SyncOrderItemsAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.GetAsync("api/sync/order-items");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Sync order items failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                ApiResponse<
                    PagedResult<
                        SyncOrderItemsResponse>>>();

        if (result?.Data?.Items == null)
            return;

        foreach (var dto in result.Data.Items)
        {
            var order =
                await _db.Orders
                    .FirstOrDefaultAsync(x => x.Token == dto.OrderToken);

            var dish =
                await _db.Dishes
                    .FirstOrDefaultAsync(x => x.Token == dto.DishToken);

            if (order == null || dish == null)
                continue;

            var existing =
                await _db.OrderItems
                    .FirstOrDefaultAsync(x => x.Token == dto.Token);

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
                    UpdatedAt = dto.UpdatedAt
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
    }
    public async Task SyncIngredientsAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.GetAsync("api/sync/ingredients");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Sync ingredients failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                ApiResponse<
                    PagedResult<
                        SyncIngredientResponse>>>();

        if (result?.Data?.Items == null)
            return;

        foreach (var dto in result.Data.Items)
        {
            var existing =
                await _db.Ingredients
                    .FirstOrDefaultAsync(x => x.Token == dto.Token);

            if (existing == null)
            {
                existing = new Ingredients
                {
                    Token = dto.Token,
                    Name = dto.Name,
                    CreatedAt = dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt
                };

                _db.Ingredients.Add(existing);
            }
            else if (dto.UpdatedAt > existing.UpdatedAt)
            {
                existing.Name = dto.Name;
                existing.UpdatedAt = dto.UpdatedAt;
            }
        }

        await _db.SaveChangesAsync();
    }
    
    public async Task SyncDishCategoriesAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.GetAsync("api/sync/dish-categories");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Sync dish categories failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                ApiResponse<
                    PagedResult<
                        GetDishCategoriesResponse>>>();

        if (result?.Data?.Items == null)
            return;

        foreach (var dto in result.Data.Items)
        {
            var existing =
                await _db.DishesCategories
                    .FirstOrDefaultAsync(x => x.Token == dto.Token);

            if (existing == null)
            {
                existing = new DishesCategories
                {
                    Token = dto.Token,
                    Name = dto.Name,
                    CreatedAt = dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt
                };

                _db.DishesCategories.Add(existing);
            }
            else if (dto.UpdatedAt > existing.UpdatedAt)
            {
                existing.Name = dto.Name;
                existing.UpdatedAt = dto.UpdatedAt;
            }
        }

        await _db.SaveChangesAsync();
    }
    public async Task SyncBansAsync(string jwt)
    {
        SetBearerToken(jwt);

        int page = 1;
        bool hasNextPage;

        do
        {
            var response =
                await HttpClient.GetAsync(
                    $"api/sync/bans?page={page}");

            if (!response.IsSuccessStatusCode)
            {
                var error =
                    await response.Content.ReadAsStringAsync();

                throw new Exception(
                    $"Sync bans failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
            }

            var result =
                await response.Content
                    .ReadFromJsonAsync<
                        ApiResponse<
                            PagedResult<
                                SyncBanResponse>>>();

            if (result?.Data?.Items == null)
                return;

            foreach (var dto in result.Data.Items)
            {
                var user =
                    await _db.Users
                        .FirstOrDefaultAsync(x => x.Token == dto.UserToken);

                if (user == null)
                    continue;

                var existing =
                    await _db.Bans
                        .Include(x => x.Statuses)
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
                        UpdatedAt = dto.UpdatedAt
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

                var statuses =
                    await _db.BanStatuses
                        .Where(x => dto.StatusTokens.Contains(x.Token))
                        .ToListAsync();

                foreach (var status in statuses)
                {
                    existing.Statuses.Add(status);
                }
            }

            await _db.SaveChangesAsync();

            hasNextPage =
                result.Data.HasNextPage;

            page++;

        } while (hasNextPage);
    }
    public async Task<SyncBootstrapResponse?>
    DownloadBootstrapManifestAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.GetAsync(
                "api/sync/bootstrap");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Download sync bootstrap failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        return await response.Content
            .ReadFromJsonAsync<
                SyncBootstrapResponse>();
    }
    public async Task SyncDishesAsync(string jwt)
    {
        SetBearerToken(jwt);

        int page = 1;
        bool hasNextPage;

        do
        {
            var response =
                await HttpClient.GetAsync(
                    $"api/sync/dishes?page={page}");

            if (!response.IsSuccessStatusCode)
            {
                var error =
                    await response.Content.ReadAsStringAsync();

                throw new Exception(
                    $"Sync dishes failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
            }

            var result =
                await response.Content
                    .ReadFromJsonAsync<
                        ApiResponse<
                            PagedResult<
                                SyncDishResponse>>>();

            if (result?.Data?.Items == null)
                return;

            foreach (var dto in result.Data.Items)
            {
                var category =
                    await _db.DishesCategories
                        .FirstOrDefaultAsync(
                            x => x.Token == dto.CategoryToken);

                if (category == null)
                    continue;

                var existing =
                    await _db.Dishes
                        .Include(x => x.Ingredients)
                        .FirstOrDefaultAsync(
                            x => x.Token == dto.Token);

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
                        UpdatedAt = dto.UpdatedAt
                    };

                    _db.Dishes.Add(existing);
                }
                else if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    existing.Name = dto.Name;
                    existing.Description = dto.Description;
                    existing.Price = dto.Price;
                    existing.AvailableFrom = dto.AvailableFrom;
                    existing.CategoryId = category.Id;
                    existing.UpdatedAt = dto.UpdatedAt;
                }

                existing.Ingredients.Clear();

                var ingredients =
                    await _db.Ingredients
                        .Where(x => dto.IngredientTokens.Contains(x.Token))
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
}