using Auth.API.Data;
using Microsoft.AspNetCore.Mvc;
using Auth.API.Interfaces;
using Auth.API.Objects;
using Auth.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Auth.API.Logic;
using System.Security.Claims;

namespace Auth.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RunnerAuthController(
                ApplicationDbContext context,
                IPasswordHasher passwordHasher,
                ISecretGenerator secretGenerator) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly ISecretGenerator _secretGenerator = secretGenerator;

        [HttpPost("register")]
        [Authorize]
        public async Task<IActionResult> RegisterRunner([FromBody] RegisterRunnerRequest request)
        {
            var user = await DoesUserExist(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (user == null)
            {
                return Unauthorized();
            }

            var rawSecret = _secretGenerator.Generate128CharSecret(); // Your method
            var hashedSecret = _passwordHasher.HashPassword(rawSecret);

            var runner = new TaskRunner
            {
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                UserUUID = user.UUID,
                SecretHash = hashedSecret
            };

            _context.TaskRunners.Add(runner);
            await _context.SaveChangesAsync();

            runner.SecretHash = rawSecret;

            return Ok(new
            {
                runner.Id,
                runner.Name,
                runner.Description,
                Secret = rawSecret, // One-time display
                runner.CreatedAt,
                runner.LastConnected
            });
        }

        [HttpPost("user-runners")]
        [Authorize]
        public async Task<IActionResult> GetUserRunners()
        {
            var user = await DoesUserExist(User.FindFirst("sub")?.Value, includeRunners: true);
            if (user == null)
            {
                return Unauthorized();
            }

            var runners = user.TaskRunners
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    r.CreatedAt,
                    r.LastConnected,
                    r.IsActive
                })
                .ToList();

            return Ok(runners);
        }


        private async Task<User?> DoesUserExist(string? userUUID, bool includeRunners = false)
        {
            if (!Guid.TryParse(userUUID, out var parsedUserUUID))
                return null;

            IQueryable<User> query = _context.Users.AsNoTracking();

            if (includeRunners)
                query = query.Include(u => u.TaskRunners);

            return await query.SingleOrDefaultAsync(u => u.UUID == parsedUserUUID);
        }
    }
}
