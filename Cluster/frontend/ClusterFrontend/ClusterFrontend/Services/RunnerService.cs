using ClusterFrontend.DTOs;
using ClusterFrontend.Interface;
using ClusterFrontend.Objects;
using System.Net.Http.Json;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace ClusterFrontend.Services
{
    public class RunnerService : IRunnerService
    {
        private readonly HttpClient _httpClient;

        // HttpClient comes pre‐configured with BaseAddress from ApiSettings
        public RunnerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        //private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

        public async Task<TaskRunner?> RequestNewRunner(RequestRunner request)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(request);

                // Create a request message to see the headers before sending
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"Auth/RunnerAuth/register")
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                // Debug: Check if authorization header will be present
                // Note: This won't actually show the header added by the handler yet
                Console.WriteLine($"Authorization header before handler: {requestMessage.Headers.Contains("Authorization")}");

                var response = await _httpClient.SendAsync(requestMessage);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Runner registration failed: {error}");
                }

                var registeredRunner = await response.Content.ReadFromJsonAsync<TaskRunner>();
                return registeredRunner;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RequestNewRunner] Error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<TaskRunner>> GetRunners()
        {
            try
            {
                var response = await _httpClient.PostAsync($"Auth/RunnerAuth/runners", null);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to get runners: {error}");
                }

                var runners = await response.Content.ReadFromJsonAsync<List<TaskRunner>>();

                return runners ?? new List<TaskRunner>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetRunners] Error: {ex.Message}");
                return new List<TaskRunner>();
            }
        }
    }
}
