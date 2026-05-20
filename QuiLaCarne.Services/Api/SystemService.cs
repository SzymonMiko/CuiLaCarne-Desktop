using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using QuiLaCarne.Models.Responses;

namespace QuiLaCarne.Services.Api;

public class SystemService : BaseApiService
{
    public SystemService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<List<string>> GetCacheListAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.GetAsync(
                "api/system/cache/list");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content
                    .ReadAsStringAsync();

            throw new Exception(
                $"Get cache list failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApiResponse<List<string>>>();

        return result?.Data ?? [];
    }
}
