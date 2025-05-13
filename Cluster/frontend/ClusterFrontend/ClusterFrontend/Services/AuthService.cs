using ClusterFrontend.DTOs;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using ClusterFrontend.Interface;

namespace ClusterFrontend.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private const string AuthApiURL = "http://gateway.api:8080/auth/UserAuth";

        public AuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
           // _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        }

        public async Task<bool> Register(UserRegisterRequest request)
        {
            try
            {
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(
                    $"{AuthApiURL}/register",
                    jsonContent);

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
            var response = await _httpClient.PostAsJsonAsync($"{AuthApiURL}/login", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserInfo?>();
            }

            return null;
        }

    }
}
