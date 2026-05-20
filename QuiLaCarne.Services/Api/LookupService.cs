using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models.DTOS;
using QuiLaCarne.Models.Lookup;
using QuiLaCarne.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QuiLaCarne.Services.Api;


public class LookupService : BaseApiService
{
    private readonly QuiLaCarneDbContext _db;

    public LookupService(
        HttpClient httpClient,
        QuiLaCarneDbContext db)
        : base(httpClient)
    {
        _db = db;
    }

    public async Task GetTableStatusesAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.GetAsync("api/table-statuses");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get table statuses failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                ApiResponse<List<TableStatusResponse>>>();

        if (result?.Data == null)
            return;

        foreach (var dto in result.Data)
        {
            var existing =
                await _db.TableStatuses
                    .FirstOrDefaultAsync(x => x.Token == dto.Token);

            if (existing == null)
            {
                existing = new TableStatus
                {
                    Token = dto.Token,
                    Name = dto.Name,
                    CreatedAt = dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt
                };

                _db.TableStatuses.Add(existing);
            }
            else if (dto.UpdatedAt > existing.UpdatedAt)
            {
                existing.Name = dto.Name;
                existing.UpdatedAt = dto.UpdatedAt;
            }
        }

        await _db.SaveChangesAsync();
    }
    public async Task<List<AllergenDictionaryResponse>>
       GetAllergensAsync(
           string jwt,
           string language = "pl")
    {
        SetBearerToken(jwt);

        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/dishes/allergens/dictionary");

        request.Headers.Add(
            "Accept-Language",
            language);

        var response =
            await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content
                    .ReadAsStringAsync();

            throw new Exception(
                $"Get allergens failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApiResponse<
                        DictionaryWrapper<
                            AllergenDictionaryResponse>>>();

        return result?.Data?.Item ?? [];
    }



    public async Task<List<BanStatusDictionaryResponse>> GetBanStatusesAsync(
    string jwt,
    string language = "pl")
    {
        SetBearerToken(jwt);

        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/ban/dictionary");

        request.Headers.Add(
            "Accept-Language",
            language);

        var response =
            await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get ban statuses failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApiResponse<
                        DictionaryWrapper<
                            BanStatusDictionaryResponse>>>();

        return result?.Data?.Item ?? [];
    }
    public async Task<List<DishCategoryDictionaryResponse>> GetDishCategoriesAsync(
    string jwt,
    string language = "pl")
    {
        SetBearerToken(jwt);

        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/dishes/dictionary");

        request.Headers.Add(
            "Accept-Language",
            language);

        var response =
            await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get dish categories failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApiResponse<
                        DictionaryWrapper<
                            DishCategoryDictionaryResponse>>>();

        return result?.Data?.Item ?? [];
    }
    public async Task<List<IngredientDictionaryResponse>> GetIngredientsAsync(
    string jwt,
    string language = "pl")
    {
        SetBearerToken(jwt);

        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/ingredients/dictionary");

        request.Headers.Add(
            "Accept-Language",
            language);

        var response =
            await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get ingredients failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApiResponse<
                        DictionaryWrapper<
                            IngredientDictionaryResponse>>>();

        return result?.Data?.Item ?? [];
    }
    public async Task<List<OrderStatusDictionaryResponse>>
    GetOrderStatusesAsync(
        string jwt,
        string language = "pl")
    {
        SetBearerToken(jwt);

        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/order/dictionary");

        request.Headers.Add(
            "Accept-Language",
            language);

        var response =
            await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content
                    .ReadAsStringAsync();

            throw new Exception(
                $"Get order statuses failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApiResponse<
                        DictionaryWrapper<
                            OrderStatusDictionaryResponse>>>();

        return result?.Data?.Item ?? [];
    }
    public async Task<List<OrderItemStatusDictionaryResponse>>
    GetOrderItemStatusesAsync(
        string jwt,
        string language = "pl")
    {
        SetBearerToken(jwt);

        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/order/item/dictionary");

        request.Headers.Add(
            "Accept-Language",
            language);

        var response =
            await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content
                    .ReadAsStringAsync();

            throw new Exception(
                $"Get order item statuses failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApiResponse<
                        DictionaryWrapper<
                            OrderItemStatusDictionaryResponse>>>();

        return result?.Data?.Item ?? [];
    }
    public async Task<List<ReservationStatusDictionaryResponse>>
    GetReservationStatusesAsync(
        string jwt,
        string language = "pl")
    {
        SetBearerToken(jwt);

        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/reservations/dictionary");

        request.Headers.Add(
            "Accept-Language",
            language);

        var response =
            await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content
                    .ReadAsStringAsync();

            throw new Exception(
                $"Get reservation statuses failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApiResponse<
                        DictionaryWrapper<
                            ReservationStatusDictionaryResponse>>>();

        return result?.Data?.Item ?? [];
    }
}

