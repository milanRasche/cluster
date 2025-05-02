using System.ComponentModel.DataAnnotations;

namespace Auth.API.Objects
{
    public class User
    {
        [Key]
        public string UUID { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserName { get; set; }

        [Required, EmailAddress]
        public string UserEmail { get; set; }

        [Required]
        public string PasswordHash { get; set; }
    }
}
