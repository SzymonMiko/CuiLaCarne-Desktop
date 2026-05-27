using System.Net.Http.Headers;
using System.Windows;

namespace QuiLaCarne.Services.Api;

public abstract class BaseApiService
{
    protected readonly HttpClient HttpClient;

    protected BaseApiService(HttpClient httpClient)
    {
        HttpClient = httpClient;

      
    }

    protected void SetBearerToken(string jwt)
    {
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            jwt
        );
        
    }
}
