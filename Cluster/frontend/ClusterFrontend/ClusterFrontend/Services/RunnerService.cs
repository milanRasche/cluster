using ClusterFrontend.DTOs;
using ClusterFrontend.Interface;
using ClusterFrontend.Objects;
using System.Runtime.CompilerServices;

namespace ClusterFrontend.Services
{
    
    public class RunnerService : IRunnerService
    {
        private readonly HttpClient _httpClient;

        public RunnerService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public Task<TaskRunner> RequestNewRunner(RequestRunner runner)
        {
            return null;
        }

        public Task<TaskRunner> RenameRunner(TaskRunner runner)
        {
            return null;
        }

        public Task DeleteRunner(TaskRunner runner)
        {
            return null;
        }
    }
}
