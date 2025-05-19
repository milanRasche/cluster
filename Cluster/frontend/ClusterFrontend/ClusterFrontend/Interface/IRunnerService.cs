using ClusterFrontend.DTOs;
using ClusterFrontend.Objects;

namespace ClusterFrontend.Interface
{
    public interface IRunnerService
    {
        public Task<TaskRunner> RequestNewRunner(RequestRunner runner);


        public Task<TaskRunner> RenameRunner(TaskRunner runner);

        public Task DeleteRunner(TaskRunner runner);

    }
}
