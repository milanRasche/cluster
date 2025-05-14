using Auth.API.Data;
using Auth.API.DTOs;
using Auth.API.Interfaces;
using Auth.API.Objects;
using Azure.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Security.Cryptography;

namespace Auth.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJWTTokenGenerator _jwtGenerator;
        private readonly IRefreshTokenGenerator _refreshGenerator;

        public UserAuthController(ApplicationDbContext context, IPasswordHasher passwordHasher, IJWTTokenGenerator jwtTokenGenerator, IRefreshTokenGenerator refreshTokenGenerator)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtGenerator = jwtTokenGenerator;
            _refreshGenerator = refreshTokenGenerator;
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
                Username = request.Username,
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

            var jwtToken = _jwtGenerator.GenerateToken(user);
            
            var refreshToken = new RefreshToken
            {
                Token = _refreshGenerator.GenerateToken(),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                UserUUID = user.UUID,
                CreationDate = DateTime.UtcNow,
                IsRevoked = false
            };

            user.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                JWTToken = jwtToken,
                RefreshToken = refreshToken.Token
            });

        }   
    }
}
