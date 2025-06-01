using System.ComponentModel.DataAnnotations;

namespace Auth.API.DTOs
{
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Refresh token required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
