using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models.DTOS;
using QuiLaCarne.Models.Lookup;
using QuiLaCarne.Models.Responses;

namespace QuiLaCarne.Services.Api;

public class LookupService : BaseApiService
{
    private readonly QuiLaCarneDbContext _db;

    public LookupService(HttpClient httpClient, QuiLaCarneDbContext db)
        : base(httpClient)
    {
        _db = db;
    }

    public async Task GetTableStatusesAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response = await HttpClient.GetAsync("api/tables/dictionary");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get table statuses failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<
            ApiResponse<DictionaryWrapper<TableStatusResponse>>
        >();

        if (result?.Data?.Item == null)
            return;

        foreach (var dto in result.Data.Item)
        {
            var name = GetDictionaryName(dto.Name, dto.NamePl, dto.NameEn);

            var existing = await _db.TableStatuses.FirstOrDefaultAsync(x => x.Token == dto.Token);

            if (existing == null)
            {
                existing = new TableStatus
                {
                    Token = dto.Token,
                    Name = name,
                    CreatedAt = dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt,
                };

                _db.TableStatuses.Add(existing);
            }
            else if (dto.UpdatedAt > existing.UpdatedAt)
            {
                existing.Name = name;
                existing.UpdatedAt = dto.UpdatedAt;
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                existing.Name = name;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<AllergenDictionaryResponse>> GetAllergensAsync(
        string jwt,
        string language = "pl"
    )
    {
        SetBearerToken(jwt);

        var request = new HttpRequestMessage(HttpMethod.Get, "api/dishes/allergens/dictionary");

        request.Headers.Add("Accept-Language", language);

        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get allergens failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<
            ApiResponse<DictionaryWrapper<AllergenDictionaryResponse>>
        >();

        return result?.Data?.Item ?? [];
    }

    public async Task<List<BanStatusDictionaryResponse>> GetBanStatusesAsync(
        string jwt,
        string language = "pl"
    )
    {
        SetBearerToken(jwt);

        var request = new HttpRequestMessage(HttpMethod.Get, "api/ban/dictionary");

        request.Headers.Add("Accept-Language", language);

        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get ban statuses failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<
            ApiResponse<DictionaryWrapper<BanStatusDictionaryResponse>>
        >();

        return result?.Data?.Item ?? [];
    }

    public async Task<List<DishCategoryDictionaryResponse>> GetDishCategoriesAsync(
        string jwt,
        string language = "pl"
    )
    {
        SetBearerToken(jwt);

        var request = new HttpRequestMessage(HttpMethod.Get, "api/dishes/dictionary");

        request.Headers.Add("Accept-Language", language);

        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get dish categories failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<
            ApiResponse<DictionaryWrapper<DishCategoryDictionaryResponse>>
        >();

        var items = result?.Data?.Item ?? [];

        foreach (var dto in items)
        {
            var existing = await _db.DishesCategories.FirstOrDefaultAsync(x =>
                x.Token == dto.Token
            );

            if (existing == null)
            {
                existing = new DishesCategories
                {
                    Token = dto.Token,
                    Name = dto.Name,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };

                _db.DishesCategories.Add(existing);
            }
            else
            {
                existing.Name = dto.Name;
            }
        }

        await _db.SaveChangesAsync();

        return items;
    }

    public async Task<List<IngredientDictionaryResponse>> GetIngredientsAsync(
        string jwt,
        string language = "pl"
    )
    {
        SetBearerToken(jwt);

        var request = new HttpRequestMessage(HttpMethod.Get, "api/ingredients/dictionary");

        request.Headers.Add("Accept-Language", language);

        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get ingredients failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<
            ApiResponse<DictionaryWrapper<IngredientDictionaryResponse>>
        >();

        return result?.Data?.Item ?? [];
    }

    public async Task<List<OrderStatusDictionaryResponse>> GetOrderStatusesAsync(
        string jwt,
        string language = "pl"
    )
    {
        SetBearerToken(jwt);

        var request = new HttpRequestMessage(HttpMethod.Get, "api/order/dictionary");

        request.Headers.Add("Accept-Language", language);

        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get order statuses failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<
            ApiResponse<DictionaryWrapper<OrderStatusDictionaryResponse>>
        >();

        return result?.Data?.Item ?? [];
    }

    public async Task<List<OrderItemStatusDictionaryResponse>> GetOrderItemStatusesAsync(
        string jwt,
        string language = "pl"
    )
    {
        SetBearerToken(jwt);

        var request = new HttpRequestMessage(HttpMethod.Get, "api/order/item/dictionary");

        request.Headers.Add("Accept-Language", language);

        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get order item statuses failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<
            ApiResponse<DictionaryWrapper<OrderItemStatusDictionaryResponse>>
        >();

        return result?.Data?.Item ?? [];
    }

    public async Task<List<ReservationStatusDictionaryResponse>> GetReservationStatusesAsync(
        string jwt,
        string language = "pl"
    )
    {
        SetBearerToken(jwt);

        var request = new HttpRequestMessage(HttpMethod.Get, "api/reservations/dictionary");

        request.Headers.Add("Accept-Language", language);

        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get reservation statuses failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<
            ApiResponse<DictionaryWrapper<ReservationStatusDictionaryResponse>>
        >();

        return result?.Data?.Item ?? [];
    }

    public async Task<bool> AddTableStatusAsync(string jwt, string namePl, string nameEn)
    {
        SetBearerToken(jwt);

        var request = new AddTableStatusRequest { NamePl = namePl, NameEn = nameEn };

        var response = await HttpClient.PostAsJsonAsync("api/tables/status/add", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Add table status failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    public async Task<bool> AddOrderItemStatusAsync(string jwt, string namePl, string nameEn)
    {
        SetBearerToken(jwt);

        var request = new AddOrderItemStatusRequest { NamePl = namePl, NameEn = nameEn };

        var response = await HttpClient.PostAsJsonAsync("api/order/item/status/add", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Add order item status failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    public async Task<SyncDictionariesResponse?> GetAllDictionariesAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response = await HttpClient.GetAsync("api/sync/dictionaries");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get dictionaries failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var body = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var apiResponse = TryDeserialize<ApiResponse<SyncDictionariesResponse>>(body, options);
        if (apiResponse?.Data != null)
            return apiResponse.Data;

        return TryDeserialize<SyncDictionariesResponse>(body, options);
    }

    public async Task<bool> AddOrderStatusAsync(string jwt, string namePl, string nameEn)
    {
        SetBearerToken(jwt);

        var request = new AddOrderStatusRequest { NamePl = namePl, NameEn = nameEn };

        var response = await HttpClient.PostAsJsonAsync("api/order/status/add", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Add order status failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    public async Task<bool> AddIngredientAsync(
        string jwt,
        string namePl,
        string nameEn,
        List<string> allergenTokens
    )
    {
        SetBearerToken(jwt);

        var request = new AddIngredientRequest
        {
            Entity = new AddEntityRequest { NamePl = namePl, NameEn = nameEn },
            AllergenTokens = allergenTokens,
        };

        var response = await HttpClient.PostAsJsonAsync("api/ingredients", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Add ingredient failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    public async Task<bool> AddDishCategoryAsync(string jwt, string namePl, string nameEn)
    {
        SetBearerToken(jwt);

        var request = new AddDishCategoryRequest { NamePl = namePl, NameEn = nameEn };

        var response = await HttpClient.PostAsJsonAsync("api/dishes/category/add", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Add dish category failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    public async Task<bool> AddAllergenAsync(string jwt, string namePl, string nameEn)
    {
        SetBearerToken(jwt);

        var request = new AddAllergenRequest { NamePl = namePl, NameEn = nameEn };

        var response = await HttpClient.PostAsJsonAsync("api/dishes/allergens/add", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Add allergen failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    public async Task<bool> DeleteTableStatusAsync(string jwt, string token)
    {
        SetBearerToken(jwt);

        var response = await HttpClient.DeleteAsync($"api/tables/status/{token}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Delete table status failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    public async Task<bool> DeleteOrderStatusAsync(string jwt, string token)
    {
        SetBearerToken(jwt);

        var response = await HttpClient.DeleteAsync($"api/order/status/{token}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Delete order status failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    public async Task<bool> DeleteOrderItemStatusAsync(string jwt, string token)
    {
        SetBearerToken(jwt);

        var response = await HttpClient.DeleteAsync($"api/order/item/status/{token}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Delete order item status failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    public async Task<bool> DeleteIngredientAsync(string jwt, string token)
    {
        SetBearerToken(jwt);

        var response = await HttpClient.DeleteAsync($"api/ingredients/{token}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Delete ingredient failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    public async Task<bool> DeleteAllergenAsync(string jwt, string allergenToken)
    {
        SetBearerToken(jwt);

        var response = await HttpClient.DeleteAsync($"api/dishes/allergen/{allergenToken}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Delete allergen failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }
    public async Task<bool> DeleteDishCategoryAsync(
    string jwt,
    string categoryToken)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.DeleteAsync(
                $"api/dishes/category/{categoryToken}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Delete dish category failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    private static string GetDictionaryName(params string[] names)
    {
        return names.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
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
