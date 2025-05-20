using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auth.API.Objects
{
    [Table("TaskRunners")]
    public class TaskRunner
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        [Required]
        [StringLength(128)] //If this is causing issues update.
        public string SecretHash { get; set; } = null!;
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastConnected { get; set; }
        public bool IsActive { get; set; } = true;
        [Required]
        public Guid UserUUID { get; set; }
        [ForeignKey(nameof(UserUUID))]
        public User User { get; set; } = null!;
    }
}
