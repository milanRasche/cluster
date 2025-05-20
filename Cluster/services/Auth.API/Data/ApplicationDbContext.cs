using Microsoft.EntityFrameworkCore;
using Auth.API.Objects;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Auth.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<TaskRunner> TaskRunners { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .ToTable("Users");

            modelBuilder.Entity<RefreshToken>()
                .ToTable("RefreshTokens");

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserUUID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskRunner>()
                .HasOne(tr => tr.User)
                .WithMany(u => u.TaskRunners)
                .HasForeignKey(tr => tr.UserUUID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
