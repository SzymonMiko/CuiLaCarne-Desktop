using QuiLaCarne.Models.DTOS;
using QuiLaCarne.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Services.Api;

public class AuthService : BaseApiService
{
    public AuthService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<LoginData?> LoginAsync(
    string username,
    string password)
    {
        var request = new
        {
            username,
            password
        };

        var response =
            await HttpClient.PostAsJsonAsync(
                "api/auth/login",
                request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Login failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        return result?.Data;
    }



    public async Task<Generate2FaResponse?> Generate2FaQrCodeAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.PostAsync(
                "api/user/2fa/generate",
                null);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Generate 2FA failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApiResponse<Generate2FaResponse>>();

        return result?.Data;
    }

    public async Task<bool> Enable2FaAsync(
        string jwt,
        string code)
    {
        SetBearerToken(jwt);

        var request =
            new Enable2FaRequest
            {
                Code = code
            };

        var response =
            await HttpClient.PostAsJsonAsync(
                "api/user/2fa/enable",
                request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Enable 2FA failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApiResponse<object>>();

        return result?.Success == true;
    }
    public async Task<LoginData?> Verify2FaAsync(
    string preAuthToken,
    int code)
    {
        var request =
            new VerifyTwoFactorRequest
            {
                PreAuthToken = preAuthToken,
                Code = code
            };

        var response =
            await HttpClient.PostAsJsonAsync(
                "api/auth/verify-2fa",
                request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Verify 2FA failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        return result?.Data;
    }
    public async Task<LoginData?> RefreshTokenAsync(
    string refreshToken)
    {
        var request =
            new RefreshTokenRequest
            {
                RefreshToken = refreshToken
            };

        var response =
            await HttpClient.PostAsJsonAsync(
                "api/auth/refresh",
                request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Refresh token failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        return result?.Data;
    }
    public async Task<bool> LogoutAsync(string jwt)
    {
        SetBearerToken(jwt);

        var response =
            await HttpClient.PostAsync(
                "api/auth/logout",
                null);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Logout failed: {(int)response.StatusCode} {response.StatusCode}\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiResponse<object>>();

        return result?.Success == true;
    }
}
