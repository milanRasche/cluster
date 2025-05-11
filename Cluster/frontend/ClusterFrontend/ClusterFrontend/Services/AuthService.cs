using ClusterFrontend.DTOs;

namespace ClusterFrontend.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private const string AuthApiURL = "https://localhost:8080/UserAuth";

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> Register(UserRegisterRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"{AuthApiURL}/register", request);
            return response.IsSuccessStatusCode;
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
