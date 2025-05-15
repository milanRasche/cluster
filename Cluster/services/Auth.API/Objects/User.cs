using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace Auth.API.Objects
{
    [Table("user")]
    public class User
    {
        [Key]
        public Guid UUID { get; set; } = Guid.NewGuid();

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
