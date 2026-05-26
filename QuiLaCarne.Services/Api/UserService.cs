using QuiLaCarne.Models.DTOS;
using QuiLaCarne.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Services.Api;
public class UserService : BaseApiService
{
    public UserService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<bool> AddEmployeeAsync(
        string jwt,
        CreateEmployeeRequest request)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.PostAsJsonAsync(
                "api/user/employee",
                request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Add employee failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }
    public async Task<bool> BanUserAsync(
    string jwt,
    string clientToken,
    string reason,
    DateTimeOffset expiresAt)
    {
        SetBearerToken(jwt);

        var request =
            new BanUserRequest
            {
                ClientToken = clientToken,
                Reason = reason,
                ExpiresAt = expiresAt
            };

        var response =
            await HttpClient.PostAsJsonAsync(
                "api/ban",
                request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Ban user failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }
    public async Task<bool> EditEmployeeAsync(
    string jwt,
    string employeeToken,
    string? email = null,
    string? userName = null)
    {
        SetBearerToken(jwt);

        var request =
            new EditEmployeeRequest
            {
                EmployeeToken = employeeToken,
                Email = email,
                UserName = userName
            };

        var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Patch,
                "api/user/employee")
            {
                Content =
                    JsonContent.Create(request)
            };

        var response =
            await HttpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Edit employee failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }
    public async Task<bool> ChangeEmployeeRoleAsync(
    string jwt,
    string employeeToken,
    bool admin)
    {
        SetBearerToken(jwt);

        var request =
            new ChangeEmployeeRoleRequest
            {
                EmployeeToken = employeeToken,
                Admin = admin
            };

        var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Patch,
                "api/user/employee/change-role")
            {
                Content =
                    JsonContent.Create(request)
            };

        var response =
            await HttpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Change employee role failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }
    public async Task<bool> ChangeEmployeePasswordAsync(
    string jwt,
    string employeeToken,
    string password,
    string confirmPassword)
    {
        SetBearerToken(jwt);

        var request =
            new ChangeEmployeePasswordRequest
            {
                EmployeeToken = employeeToken,
                Password = password,
                ConfirmPassword = confirmPassword
            };

        var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Patch,
                "api/user/employee/change-password")
            {
                Content =
                    JsonContent.Create(request)
            };

        var response =
            await HttpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Change employee password failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }
    public async Task<bool> ChangeEmployeeAvailabilityAsync(
    string jwt,
    string employeeToken,
    bool available)
    {
        SetBearerToken(jwt);

        var request =
            new ChangeEmployeeAvailabilityRequest
            {
                EmployeeToken = employeeToken,
                Available = available
            };

        var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Patch,
                "api/user/employee/change-availability")
            {
                Content =
                    JsonContent.Create(request)
            };

        var response =
            await HttpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Change employee availability failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }
    public async Task<bool> DeleteEmployeeAsync(
    string jwt,
    string employeeToken)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.DeleteAsync(
                $"api/user/employee/{employeeToken}/delete");

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Delete employee failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }
}
