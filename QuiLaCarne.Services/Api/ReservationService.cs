using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Text.Json;
using QuiLaCarne.Models.DTOS;
using QuiLaCarne.Models.Responses;
namespace QuiLaCarne.Services.Api;

public class ReservationService : BaseApiService
{
    public ReservationService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<ReservationDetailsResponse?> GetReservationDetailsAsync(
        string jwt,
        string reservationToken)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.GetAsync(
                $"api/reservations/{reservationToken}");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get reservation details failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApiResponse<
                        ReservationDetailsResponse>>();

        return result?.Data;
    }

    public async Task<List<SyncTableResponse>> GetRestaurantTablesAsync(
        string jwt,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        string language = "pl")
    {
        SetBearerToken(jwt);

        var url =
            "api/tables" +
            BuildQuery(
                ("startTime", startTime?.ToString("O")),
                ("endTime", endTime?.ToString("O")));

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept-Language", language);

        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get tables failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var body = await response.Content.ReadAsStringAsync();

        return ReadItems<SyncTableResponse>(body);
    }

    public async Task<List<ReservationDetailsResponse>> GetUserReservationsHistoryAsync(
        string jwt,
        int page = 1,
        int size = 10,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        string? statusToken = null)
    {
        SetBearerToken(jwt);

        var url =
            "api/reservations" +
            BuildQuery(
                ("page", page.ToString()),
                ("size", size.ToString()),
                ("fromDate", fromDate?.ToString("O")),
                ("toDate", toDate?.ToString("O")),
                ("statusToken", statusToken));

        var response = await HttpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get reservation history failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var body = await response.Content.ReadAsStringAsync();

        return ReadItems<ReservationDetailsResponse>(body);
    }

    public async Task<bool> AddTableAsync(
    string jwt,
    int tableNumber,
    int capacity)
    {
        SetBearerToken(jwt);

        var request =
            new CreateTableRequest
            {
                TableNumber = tableNumber,
                Capacity = capacity
            };

        var response =
            await HttpClient.PostAsJsonAsync(
                "api/tables",
                request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Add table failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }
    public async Task<bool> DeleteTableAsync(
    string jwt,
    string tableToken)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.DeleteAsync(
                $"api/tables/{tableToken}/delete");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Delete table failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    private static string BuildQuery(params (string Name, string? Value)[] parameters)
    {
        var values =
            parameters
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x =>
                    $"{Uri.EscapeDataString(x.Name)}={Uri.EscapeDataString(x.Value!)}")
                .ToList();

        return values.Count == 0 ? "" : "?" + string.Join("&", values);
    }

    private static List<T> ReadItems<T>(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var paged = TryDeserialize<ApiResponse<PagedResult<T>>>(json, options);
        if (paged?.Data?.Items != null)
            return paged.Data.Items;

        var list = TryDeserialize<ApiResponse<List<T>>>(json, options);
        if (list?.Data != null)
            return list.Data;

        return TryDeserialize<List<T>>(json, options) ?? [];
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
