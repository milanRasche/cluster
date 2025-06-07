using Auth.API.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Auth.API.Tests
{
    public class MigrationFileTests
    {
        private const string ConnectionString = "Server=localhost,1433;Database=MigrationTestDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";

        [Fact]
        public async Task CanApplyMigrationsToSqlServer()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            using var context = new ApplicationDbContext(options);

            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();

            var canConnect = await context.Database.CanConnectAsync();
            Assert.True(canConnect);
        }
    }
}
