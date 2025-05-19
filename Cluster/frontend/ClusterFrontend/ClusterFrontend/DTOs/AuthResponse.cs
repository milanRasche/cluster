using System.Text.Json.Serialization;

namespace ClusterFrontend.DTOs
{
    public class AuthResponse
    {
        [JsonPropertyName("jwtToken")]
        public string? JWTToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }
    }
}
