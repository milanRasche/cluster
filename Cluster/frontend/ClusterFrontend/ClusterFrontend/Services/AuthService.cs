using ClusterFrontend.DTOs;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using ClusterFrontend.Interface;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.AspNetCore.Identity.Data;

namespace ClusterFrontend.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private const string AuthApiURL = "http://gateway.api:8080/auth/UserAuth";

        public AuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(AuthApiURL);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ClusterFrontend");
        }
        
        public async Task<bool> Register(UserRegisterRequest request)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(request);

                var response = await _httpClient.PostAsync(
                    "http://gateway.api:8080/auth/UserAuth/register",
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

        public async Task<UserInfo?> Login(UserLoginRequest request)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(request);

                var response = await _httpClient.PostAsync(
                    $"/login",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json")
                );

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserInfo?>();
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

        public async Task<UserInfo?> RefreshToken(RefreshRequest refreshRequest)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(refreshRequest);

                var response = await _httpClient.PostAsync(
                    $"/refresh-token",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json")
                );

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserInfo?>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Token refresh failed with status {response.StatusCode}: {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"API request failed: {ex.Message}");
                return null;
            }
        }
    }
}
