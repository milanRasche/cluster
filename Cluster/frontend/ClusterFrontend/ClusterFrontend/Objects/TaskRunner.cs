using Microsoft.AspNetCore.DataProtection;

namespace ClusterFrontend.Objects
{
    public class TaskRunner
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string? Secret { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastConnected { get; set; }
    }
}