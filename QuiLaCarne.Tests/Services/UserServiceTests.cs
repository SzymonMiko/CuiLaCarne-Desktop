using QuiLaCarne.Models.DTOS;
using QuiLaCarne.Services.Api;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class UserServiceTests
{
    [Fact]
    public async Task AddEmployeeAsync_PostsRegisterPayload()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new UserService(httpClient);

        var added = await service.AddEmployeeAsync(
            "jwt-token",
            new CreateEmployeeRequest
            {
                Admin = true,
                Register = new RegisterRequest
                {
                    Username = "worker",
                    Email = "worker@example.com",
                    Password = "Secret123!",
                    ConfirmPassword = "Secret123!"
                }
            });

        Assert.True(added);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/user/employee", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);

        var json = await ServiceTestHelpers.ReadJsonAsync(request);
        Assert.True(json.GetProperty("admin").GetBoolean());
        Assert.Equal("worker", json.GetProperty("register").GetProperty("username").GetString());
        Assert.Equal("worker@example.com", json.GetProperty("register").GetProperty("email").GetString());
    }

    [Fact]
    public async Task ChangeEmployeeRoleAsync_PatchesRoleEndpoint()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new UserService(httpClient);

        var changed = await service.ChangeEmployeeRoleAsync("jwt-token", "employee-token", admin: true);

        Assert.True(changed);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("/api/user/employee/change-role", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);

        var json = await ServiceTestHelpers.ReadJsonAsync(request);
        Assert.Equal("employee-token", json.GetProperty("employeeToken").GetString());
        Assert.True(json.GetProperty("admin").GetBoolean());
    }

    [Fact]
    public async Task ChangeEmployeePasswordAsync_PatchesPasswordEndpoint()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new UserService(httpClient);

        var changed = await service.ChangeEmployeePasswordAsync(
            "jwt-token",
            "employee-token",
            "NewSecret123!",
            "NewSecret123!");

        Assert.True(changed);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("/api/user/employee/change-password", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);

        var json = await ServiceTestHelpers.ReadJsonAsync(request);
        Assert.Equal("employee-token", json.GetProperty("employeeToken").GetString());
        Assert.Equal("NewSecret123!", json.GetProperty("password").GetString());
        Assert.Equal("NewSecret123!", json.GetProperty("confirmPassword").GetString());
    }

    [Fact]
    public async Task ChangeEmployeeAvailabilityAsync_PatchesAvailabilityEndpoint()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new UserService(httpClient);

        var changed = await service.ChangeEmployeeAvailabilityAsync(
            "jwt-token",
            "employee-token",
            available: false);

        Assert.True(changed);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("/api/user/employee/change-availability", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);

        var json = await ServiceTestHelpers.ReadJsonAsync(request);
        Assert.Equal("employee-token", json.GetProperty("employeeToken").GetString());
        Assert.False(json.GetProperty("available").GetBoolean());
    }

    [Fact]
    public async Task BanUserAsync_PostsBanPayload()
    {
        var expiresAt = DateTimeOffset.Parse("2026-06-10T12:00:00+00:00");
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new UserService(httpClient);

        var banned = await service.BanUserAsync("jwt-token", "client-token", "spam", expiresAt);

        Assert.True(banned);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/ban", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);

        var json = await ServiceTestHelpers.ReadJsonAsync(request);
        Assert.Equal("client-token", json.GetProperty("clientToken").GetString());
        Assert.Equal("spam", json.GetProperty("reason").GetString());
        Assert.Equal(expiresAt, json.GetProperty("expiresAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task DeleteEmployeeAsync_CallsDeleteEndpoint()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(ServiceTestHelpers.SuccessJson)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new UserService(httpClient);

        var deleted = await service.DeleteEmployeeAsync("jwt-token", "employee-token");

        Assert.True(deleted);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/api/user/employee/employee-token/delete", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);
    }

    [Fact]
    public async Task GetRolesAsync_ReadsPagedRoles()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(FakeHttpMessageHandler.Json(
                """
                {
                  "success": true,
                  "message": "",
                  "statusCode": 200,
                  "errorMessages": [],
                  "data": {
                    "items": [
                      { "token": "ROLE_MANAGER", "name": "Manager" }
                    ],
                    "hasNextPage": false
                  }
                }
                """)));
        using var httpClient = ServiceTestHelpers.CreateClient(handler);
        var service = new UserService(httpClient);

        var roles = await service.GetRolesAsync("jwt-token");

        var role = Assert.Single(roles);
        Assert.Equal("ROLE_MANAGER", role.Token);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("/api/sync/roles", request.RequestUri?.PathAndQuery);
        ServiceTestHelpers.AssertBearer(request);
    }
}
