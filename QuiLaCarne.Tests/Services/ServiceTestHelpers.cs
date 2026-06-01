using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using Xunit;

namespace QuiLaCarne.Tests.Services;

internal static class ServiceTestHelpers
{
    public const string SuccessJson =
        """{"success":true,"data":null,"message":"","statusCode":200,"errorMessages":[]}""";

    public static HttpClient CreateClient(FakeHttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
    }

    public static QuiLaCarneDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<QuiLaCarneDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        return new QuiLaCarneDbContext(options);
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpRequestMessage request)
    {
        var body = await request.Content!.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    public static void AssertBearer(HttpRequestMessage request, string jwt = "jwt-token")
    {
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal(jwt, request.Headers.Authorization?.Parameter);
    }
}
