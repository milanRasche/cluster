using Auth.API.Data;
using Auth.API.DTOs;
using Auth.API.Objects;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public UserAuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.UserEmail == request.UserEmail))
            {
                return BadRequest("User already exists.");
            }

            var hashedPassword =  HashPassword(request.Password);

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
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            return Ok(new { user.UUID, user.UserName, user.UserEmail });
        }



        private string HashPassword(string password)
        {
            return "";
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}
