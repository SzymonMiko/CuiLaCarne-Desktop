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

public class ReportService : BaseApiService
{
    public ReportService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<bool> CreateGuestReportAsync(
        string jwt,
        string clientToken,
        string reason)
    {
        SetBearerToken(jwt);

        var request =
            new CreateGuestReportRequest
            {
                ClientToken = clientToken,
                Reason = reason
            };

        var response =
            await HttpClient.PostAsJsonAsync(
                "api/report",
                request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Create guest report failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }
    public async Task<bool> ChangeReportStatusAsync(
    string jwt,
    string reportToken,
    bool accepted,
    DateTimeOffset? expiresAt)
    {
        SetBearerToken(jwt);

        var request =
            new ChangeReportStatusRequest
            {
                ReportToken = reportToken,
                Accepted = accepted,
                ExpiresAt = expiresAt
            };

        var response =
            await HttpClient.PutAsJsonAsync(
                "api/report/change-status",
                request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Change report status failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }

    public async Task<List<SyncGuestReportResponse>> GetReportsAsync(
        string jwt,
        int page = 1,
        int size = 20)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.GetAsync(
                $"api/sync/reports?page={page}&size={size}");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get reports failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var body =
            await response.Content.ReadAsStringAsync();

        return ReadItems<SyncGuestReportResponse>(body);
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
