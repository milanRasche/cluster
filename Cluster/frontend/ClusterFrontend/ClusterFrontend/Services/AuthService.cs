using ClusterFrontend.DTOs;
using System.Text.Json;
using System.Text;
using ClusterFrontend.Interface;

namespace ClusterFrontend.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private const string AuthApiURL = "http://gateway.api:8080/auth/UserAuth";

        public AuthService(IHttpClientFactory httpClientFactory)
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
                    $"{AuthApiURL}/register",
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
                    $"{AuthApiURL}/login",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json")
                );

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AuthResponse?>();
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
    }
}
