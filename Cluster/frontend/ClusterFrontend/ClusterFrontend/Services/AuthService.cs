using ClusterFrontend.DTOs;
using System.Text.Json;
using System.Text;
using ClusterFrontend.Interface;
using Microsoft.JSInterop;

namespace ClusterFrontend.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // HttpClient comes pre‐configured with BaseAddress from ApiSettings
        public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> Register(UserRegisterRequest request)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(request);

                var response = await _httpClient.PostAsync(
                    $"Auth/UserAuth/register",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json")
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"API returned status {response.StatusCode}: {errorContent}");
                }

                return true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"API request failed: {ex.Message}");
                return false;
            }
        }

        public async Task<AuthResponse?> Login(UserLoginRequest request)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(request);
                var response = await _httpClient.PostAsync(
                    $"Auth/UserAuth/login",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json")
                );
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse?>();

                    if (authResponse != null)
                    {
                        await SetCookiesViaJsInterop(authResponse);

                        return authResponse;
                    }

                    return authResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Login failed with status {response.StatusCode}: {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"API request failed: {ex.Message}");
                return null;
            }
        }

        public async Task Logout()
        {
            try
            {
                string refreshToken = null;
                try
                {
                    refreshToken = await _jsRuntime.InvokeAsync<string>("cookieHelpers.getCookie", "RefreshToken");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued at this time"))
                {
                    _httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue("RefreshToken", out refreshToken);
                }

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var content = new StringContent(
                        JsonSerializer.Serialize(new { RefreshToken = refreshToken }),
                        Encoding.UTF8,
                        "application/json"
                    );

                    // Call the correct logout endpoint
                    var response = await _httpClient.PostAsync(
                        $"UserAuth/logout",
                        content);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Logout API call failed: {response.StatusCode}");
                    }
                }

                try
                {
                    await _jsRuntime.InvokeVoidAsync("cookieHelpers.removeCookie", "JWTToken");
                    await _jsRuntime.InvokeVoidAsync("cookieHelpers.removeCookie", "RefreshToken");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued at this time"))
                {
                    if (_httpContextAccessor.HttpContext != null)
                    {
                        _httpContextAccessor.HttpContext.Response.Cookies.Delete("JWTToken");
                        _httpContextAccessor.HttpContext.Response.Cookies.Delete("RefreshToken");
                    }
                }

                Console.WriteLine("Logout successful: tokens revoked and cookies deleted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logout: {ex.Message}");

                try
                {
                    await _jsRuntime.InvokeVoidAsync("cookieHelpers.removeCookie", "JWTToken");
                    await _jsRuntime.InvokeVoidAsync("cookieHelpers.removeCookie", "RefreshToken");
                }
                catch
                {
                    if (_httpContextAccessor.HttpContext != null)
                    {
                        _httpContextAccessor.HttpContext.Response.Cookies.Delete("JWTToken");
                        _httpContextAccessor.HttpContext.Response.Cookies.Delete("RefreshToken");
                    }
                }

                throw; // Re-throw the exception
            }
        }

        private async Task SetCookiesViaJsInterop(AuthResponse authResponse)
        {
            try
            {
                // Using the cookieHelper.js functions
                await _jsRuntime.InvokeVoidAsync(
                    "cookieHelpers.setCookie",
                    "JWTToken",
                    authResponse.JWTToken,
                    7, // days valid
                    "Strict" // SameSite attribute
                );

                await _jsRuntime.InvokeVoidAsync(
                    "cookieHelpers.setCookie",
                    "RefreshToken",
                    authResponse.RefreshToken,
                    30, // days valid
                    "Strict" // SameSite attribute
                );

                Console.WriteLine("Cookies set successfully via JS Interop");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting cookies via JS Interop: {ex.Message}");
                throw;
            }
        }
    }
}
