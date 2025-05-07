using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace Auth.API.Objects
{
    [Table("user", Schema = "dbo")]
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
