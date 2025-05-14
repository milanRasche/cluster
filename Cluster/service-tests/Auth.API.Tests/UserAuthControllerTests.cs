using Auth.API.Controllers;
using Auth.API.Data;
using Auth.API.Logic;
using Auth.API.Interfaces;
using Auth.API.DTOs;
using Auth.API.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace Auth.API.Tests
{
    public class UserAuthControllerTests
    {
        private readonly UserAuthController _controller;
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJWTTokenGenerator _jwtTokenGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly IConfiguration _configuration;

        public UserAuthControllerTests()
        {
            _context = TestDbContextFactory.Create();
            _passwordHasher = new SHA256PasswordHasher();
            var inMemorySettings = new Dictionary<string, string>
        {
            { "Jwt:Key", "SuperSecretKeyForTesting123" }
        };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _jwtTokenGenerator = new JWTTokenGenerator(_configuration);
            _refreshTokenGenerator = new RefreshTokenGenerator();
            _refreshTokenGenerator = new RefreshTokenGenerator();
            _controller = new UserAuthController(_context, _passwordHasher, _jwtTokenGenerator, _refreshTokenGenerator);
        }

        [Fact]
        public async Task Register_ShouldCreateNewUser_WhenEmailsNotTaken()
        {
            // Arrange
            var request = new UserRegisterRequest
            {
                Username = "TestUser",
                UserEmail = "test@example.com",
                Password = "Password123"
            };

            // Act
            var result = await _controller.Register(request) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserEmail == request.UserEmail);
            Assert.NotNull(user);
        }

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenEmailAlreadyExists()
        {
            // Arrange
            _context.Users.Add(new User { Username = "Existing", UserEmail = "exists@example.com", PasswordHash = "hash" });
            await _context.SaveChangesAsync();

            var request = new UserRegisterRequest
            {
                Username = "NewUser",
                UserEmail = "exists@example.com",
                Password = "Password123"
            };

            // Act
            var result = await _controller.Register(request) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("User already exists.", result.Value);
        }

        //[Fact]
        //public async Task Login_ShouldReturnUser_WhenCredentialsMatchUser()
        //{
        //    //Arange
        //    var email = "validuser@example.com";
        //    var password = "Password123";
        //    var hash = "";

        //    _context.Users.Add(new User { Username = "Validuser", UserEmail = email, PasswordHash = hash });
        //    await _context.SaveChangesAsync();

        //    var request = new UserLoginRequest
        //    {
        //        UserEmail = email,
        //        Password = password
        //    };

        //    //Act
        //    var result = await _controller.Login(request) as OkObjectResult;

        //    //Assert
        //    Assert.NotNull(result);
        //    Assert.Equal(200, result.StatusCode);
        //    var response = result.Value as dynamic;
        //    Assert.Equal(email, (string)response!.UserEmail);
        //}
    }
}
