using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auth.API.Objects
{
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Token { get; set; } = string.Empty;
        [Required]
        public DateTime ExpiryDate { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        [Required]
        public bool IsRevoked { get; set; }
        [Required]
        public Guid UserUUID { get; set; }
        [ForeignKey(nameof(UserUUID))]
        public User User { get; set; }
    }
}
