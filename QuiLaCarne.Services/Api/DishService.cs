using QuiLaCarne.Models.DTOS;
using QuiLaCarne.Models.Responses;
using QuiLaCarne.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QuiLaCarne.Services.Api;

public class DishService : BaseApiService
{
    public DishService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<List<DishMenuResponse>>
      GetFullRestaurantMenuAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.GetAsync(
                "api/dishes");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content
                    .ReadAsStringAsync();

            throw new Exception(
                $"Get menu failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApiResponse<
                        PagedResult<
                            DishMenuResponse>>>();

        return result?.Data?.Items ?? [];
    }
}