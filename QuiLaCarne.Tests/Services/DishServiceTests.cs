using System.Net;
using QuiLaCarne.Services.Api;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class DishServiceTests
{
    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".webp", "image/webp")]
    public async Task EditDishAsync_SendsCorrectPhotoContentType(
        string extension,
        string expectedContentType)
    {
        using var photo = new TemporaryPhotoFile(extension);
        string? actualContentType = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            var multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
            var photoContent = Assert.Single(multipart, part =>
                part.Headers.ContentType?.MediaType == expectedContentType);
            actualContentType = photoContent.Headers.ContentType?.MediaType;

            return Task.FromResult(FakeHttpMessageHandler.Json(
                """{"success":true,"data":null,"message":"","statusCode":200,"errorMessages":[]}"""));
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new DishService(httpClient);

        var result = await service.EditDishAsync(
            "jwt-token",
            "dish-token",
            photoPath: photo.Path);

        Assert.True(result);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("/api/dishes", request.RequestUri?.PathAndQuery);
        Assert.Equal(expectedContentType, actualContentType);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("jwt-token", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task EditDishAsync_RejectsInvalidPhotoExtensionBeforeCallingApi()
    {
        using var photo = new TemporaryPhotoFile(".gif");
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """{"success":true,"data":null,"message":"","statusCode":200,"errorMessages":[]}""")));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new DishService(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EditDishAsync("jwt-token", "dish-token", photoPath: photo.Path));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task DeleteDishAsync_CallsDeleteEndpointWithToken()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """{"success":true,"data":null,"message":"","statusCode":200,"errorMessages":[]}""")));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new DishService(httpClient);

        var result = await service.DeleteDishAsync("jwt-token", "dish-token");

        Assert.True(result);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/api/dishes/dish-token", request.RequestUri?.PathAndQuery);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("jwt-token", request.Headers.Authorization?.Parameter);
    }

    private sealed class TemporaryPhotoFile : IDisposable
    {
        public string Path { get; }

        public TemporaryPhotoFile(string extension)
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"{Guid.NewGuid():N}{extension}");

            File.WriteAllBytes(Path, [1, 2, 3, 4]);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
