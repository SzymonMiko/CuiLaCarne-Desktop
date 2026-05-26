using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
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


}
