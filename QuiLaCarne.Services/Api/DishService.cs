using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using QuiLaCarne.Models.DTOS;
using QuiLaCarne.Models.Responses;

namespace QuiLaCarne.Services.Api;

public class DishService : BaseApiService
{
    public DishService(HttpClient httpClient)
        : base(httpClient) { }

    public async Task<List<DishMenuResponse>> GetFullRestaurantMenuAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response = await HttpClient.GetAsync("api/dishes");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get menu failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<
            ApiResponse<PagedResult<DishMenuResponse>>
        >();

        return result?.Data?.Items ?? [];
    }

    public async Task ChangeDishAvailabilityAsync(
        string jwt,
        string dishToken,
        bool available,
        string? unavailableReason
    )
    {
        SetBearerToken(jwt);

        var request = new ChangeDishAvailabilityRequest
        {
            Token = dishToken,
            UnavailableReason = available ? null : unavailableReason,
            Available = available,
        };

        var response = await HttpClient.PatchAsJsonAsync("api/dishes", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Change dish availability failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }
    }

    public async Task AddDishAsync(
        string jwt,
        string name,
        int price,
        string categoryToken,
        List<string> ingredientTokens,
        string? photoPath
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/dishes");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var form = new MultipartFormDataContent();

        form.Add(new StringContent(name), "name");
        form.Add(new StringContent(price.ToString()), "price");
        form.Add(new StringContent(categoryToken), "categoryToken");

        foreach (var token in ingredientTokens)
        {
            form.Add(new StringContent(token), "ingredientTokens");
        }

        if (!string.IsNullOrWhiteSpace(photoPath) && File.Exists(photoPath))
        {
            var stream = File.OpenRead(photoPath);
            var fileContent = new StreamContent(stream);

            form.Add(fileContent, "photo", Path.GetFileName(photoPath));
        }

        request.Content = form;

        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }
    }

    public async Task<bool> EditDishAsync(
        string jwt,
        string dishToken,
        string? newName = null,
        string? categoryToken = null,
        int? price = null,
        List<string>? ingredientTokens = null,
        string? photoPath = null
    )
    {
        SetBearerToken(jwt);

        using var form = new MultipartFormDataContent();

        form.Add(new StringContent(dishToken), "dishToken");

        if (!string.IsNullOrWhiteSpace(newName))
        {
            form.Add(new StringContent(newName), "newName");
        }

        if (!string.IsNullOrWhiteSpace(categoryToken))
        {
            form.Add(new StringContent(categoryToken), "categoryToken");
        }

        if (price.HasValue)
        {
            form.Add(new StringContent(price.Value.ToString()), "price");
        }

        if (ingredientTokens != null)
        {
            foreach (var token in ingredientTokens)
            {
                form.Add(new StringContent(token), "ingredientTokens");
            }
        }

        if (!string.IsNullOrWhiteSpace(photoPath) && File.Exists(photoPath))
        {
            var stream = File.OpenRead(photoPath);

            var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                "image/jpeg"
            );

            form.Add(fileContent, "photo", Path.GetFileName(photoPath));
        }

        var response = await HttpClient.PutAsync("api/dishes", form);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Edit dish failed: {(int)response.StatusCode} {response.StatusCode}\n{error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    public async Task<bool> DeleteDishAsync(string jwt, string dishToken)
    {
        SetBearerToken(jwt);

        var response = await HttpClient.DeleteAsync($"api/dishes/{dishToken}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return result?.Success == true;
    }

   
}
