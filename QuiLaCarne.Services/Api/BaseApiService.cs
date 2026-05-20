using System.Net.Http.Headers;

namespace QuiLaCarne.Services.Api;

public abstract class BaseApiService
{
    protected readonly HttpClient HttpClient;

    protected BaseApiService(
        HttpClient httpClient)
    {
        HttpClient = httpClient;

        HttpClient.BaseAddress =
            new Uri(
                "https://api.quilacarne.com.pl/");
    }

    protected void SetBearerToken(string token)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }
}
