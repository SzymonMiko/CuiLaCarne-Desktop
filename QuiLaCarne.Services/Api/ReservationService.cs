using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http.Json;
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
}
