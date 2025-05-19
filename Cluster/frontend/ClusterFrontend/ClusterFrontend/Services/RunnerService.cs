using ClusterFrontend.DTOs;
using ClusterFrontend.Interface;
using ClusterFrontend.Objects;
using System.Runtime.CompilerServices;

namespace ClusterFrontend.Services
{
    public class RunnRunnerService(
        IHttpClientFactory httpClientFactory
        ) : IRunnerService
    {
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

        public Task<TaskRunner> RequestNewRunner(RequestRunner runner)
        {
            var abc = _httpClient;
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
