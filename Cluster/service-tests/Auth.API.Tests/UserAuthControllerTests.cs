using Auth.API.Controllers;
using Auth.API.Data;
using Auth.API.DTOs;
using Auth.API.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Xunit;

namespace Auth.API.Tests
{
    public class UserAuthControllerTests
    {
        private readonly UserAuthController _controller;
        private readonly ApplicationDbContext _context;

        public UserAuthControllerTests()
        {
            _context = TestDbContextFactory.Create();
            _controller = new UserAuthController(_context);
        }

        [Fact]
        public async Task Register_ShouldCreateNewUser_WhenEmailsNotTaken()
        {
            // Arrange
            var request = new UserRegisterRequest
            {
                UserName = "TestUser",
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
            _context.Users.Add(new User { UserName = "Existing", UserEmail = "exists@example.com", PasswordHash = "hash" });
            await _context.SaveChangesAsync();

            var request = new UserRegisterRequest
            {
                UserName = "NewUser",
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

        [Fact]
        public async Task Login_ShouldReturnUser_WhenCredentialsMatchUser()
        {
            //Arange
            var email = "validuser@example.com";
            var password = "Password123";
            var hash = "";

            _context.Users.Add(new User { UserName = "Validuser", UserEmail = email, PasswordHash = hash });
            await _context.SaveChangesAsync();

            var request = new UserLoginRequest
            {
                UserEmail = email,
                Password = password
            };

            //Act
            var result = await _controller.Login(request) as OkObjectResult;

            //Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            var response = result.Value as dynamic;
            Assert.Equal(email, (string)response!.UserEmail);
        }
    }
}
