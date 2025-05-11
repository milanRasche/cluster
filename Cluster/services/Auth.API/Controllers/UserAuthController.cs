using Auth.API.Data;
using Auth.API.DTOs;
using Auth.API.Interfaces;
using Auth.API.Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Auth.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public UserAuthController(ApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.UserEmail == request.UserEmail))
            {
                return BadRequest("User already exists.");
            }

            var hashedPassword = _passwordHasher.HashPassword(request.Password);

            var user = new User
            {
                UserName = request.UserName,
                UserEmail = request.UserEmail,
                PasswordHash = hashedPassword,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { user.UUID, user.UserEmail });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserEmail == request.UserEmail);
            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            return Ok(new { user.UUID, user.UserName, user.UserEmail });
        }
    }
}
