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
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace Auth.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserAuthController(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJWTTokenGenerator jwtGenerator,
        IRefreshTokenGenerator refreshGenerator) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IJWTTokenGenerator _jwtGenerator = jwtGenerator;
        private readonly IRefreshTokenGenerator _refreshGenerator = refreshGenerator;
        


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
            try
            {
                var user = await _context.Users
                    .AsNoTracking() // Get the user without tracking
                    .SingleOrDefaultAsync(u => u.UserEmail == request.UserEmail);

                if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                    return Unauthorized("Invalid credentials.");

                var jwtToken = _jwtGenerator.GenerateToken(user);
                var refreshToken = new RefreshToken
                {
                    Token = _refreshGenerator.GenerateToken(),
                    ExpiryDate = DateTime.UtcNow.AddDays(2),
                    UserUUID = user.UUID,
                    CreationDate = DateTime.UtcNow,
                    IsRevoked = false
                };

                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    JWTToken = jwtToken,
                    RefreshToken = refreshToken.Token
                });
            }
            catch (Exception ex)
            {
                // Log the full exception for debugging
                Console.WriteLine($"Login error: {ex}");

                return StatusCode(500, new
                {
                    Error = "An error occurred during login. Please try again later.",
                    Details = ex.Message
                });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                    return BadRequest("Token required.");

                var refreshToken = await _context.RefreshTokens
                    .Include(r => r.User)
                    .SingleOrDefaultAsync(r => r.Token == request.RefreshToken);

                if (refreshToken == null)
                    return Unauthorized("Token invalid");

                if (refreshToken.ExpiryDate < DateTime.UtcNow)
                    return Unauthorized("Token Expired");

                if (refreshToken.IsRevoked)
                    return Unauthorized("Token Revoked");

                var user = refreshToken.User;
                var newJwtToken = _jwtGenerator.GenerateToken(user);
                var newRefreshToken = new RefreshToken
                {
                    Token = _refreshGenerator.GenerateToken(),
                    ExpiryDate = DateTime.UtcNow.AddDays(2),
                    UserUUID = user.UUID,
                    CreationDate = DateTime.UtcNow,
                    IsRevoked = false
                };

                refreshToken.IsRevoked = true;

                _context.RefreshTokens.Add(newRefreshToken);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    JWTToken = newJwtToken
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Refresh token error: {ex}");

                return StatusCode(500, new
                {
                    Error = "An error occurred while refreshing the token.",
                    Details = ex.Message
                });
            }
        }
    }
}
