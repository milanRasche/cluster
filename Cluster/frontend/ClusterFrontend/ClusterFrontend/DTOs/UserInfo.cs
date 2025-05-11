namespace ClusterFrontend.DTOs
{
    public class UserInfo
    {
        public Guid UUID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }
}
