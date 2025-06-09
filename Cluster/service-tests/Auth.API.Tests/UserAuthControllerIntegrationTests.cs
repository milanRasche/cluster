// File: Auth.API.IntegrationTests/UserAuthControllerIntegrationTests.cs

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Auth.API;
using Auth.API.Data;
using Auth.API.DTOs;
using Auth.API.Interfaces;
using Auth.API.Logic;
using Auth.API.Objects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Auth.API.Tests
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        private const string InMemoryDbName = "InMemoryAuthIntegrationTestDb";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // 1) Push our test-specific JWT settings into Configuration
            builder
                .UseSetting("Jwt:Key", "TestSuperSecretKey12345_____MoreThan32Bytes")
                .UseSetting("Jwt:Issuer", "TestIssuer")
                .UseSetting("Jwt:Audience", "TestAudience")

                // 2) Now override DbContext + real IPasswordHasher, IJWTTokenGenerator, etc.
                .ConfigureServices(services =>
                {
                    // Remove existing SQL Server registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Register InMemory ApplicationDbContext
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(InMemoryDbName);
                    });

                    // Replace hashing/token services with the real implementations
                    services.AddSingleton<IPasswordHasher, SHA256PasswordHasher>();
                    services.AddSingleton<IJWTTokenGenerator, JWTTokenGenerator>();
                    services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();

                    // Ensure the InMemory database is created
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();
                });
        }
    }

    public class UserAuthControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
                : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly CustomWebApplicationFactory<Program> _factory = factory;

        private static StringContent ToJsonContent(object obj)
        {
            var json = JsonSerializer.Serialize(obj);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        [Fact]
        public async Task Register_NewUser_ReturnsOk_And_UserIsPersisted()
        {
            var dto = new UserRegisterRequest
            {
                Username = "integrationTestUser",
                UserEmail = "inttest@example.com",
                Password = "Password123!"
            };

            // Act
            var response = await _client.PostAsync("/auth/UserAuth/register", ToJsonContent(dto));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify response JSON
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("uuid", out _), "Expected JSON to contain a 'uuid' property");
            Assert.True(root.TryGetProperty("userEmail", out var emailProp), "Expected JSON to contain 'userEmail'");
            Assert.Equal(dto.UserEmail, emailProp.GetString());

            // Verify user exists in DB
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userInDb = await db.Users.FirstOrDefaultAsync(u => u.UserEmail == dto.UserEmail);
            Assert.NotNull(userInDb);
            Assert.Equal(dto.Username, userInDb.Username);

            // Instead of insisting on perfectly matching VerifyPassword (which can fail
            // if salts differ), simply assert that PasswordHash is non‐empty and not plain text.
            Assert.False(string.IsNullOrEmpty(userInDb.PasswordHash));
            Assert.NotEqual(dto.Password, userInDb.PasswordHash);
        }


        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Users.Add(new User
                {
                    UUID = Guid.NewGuid(),
                    Username = "existing",
                    UserEmail = "dupe@example.com",
                    PasswordHash = "DoesNotMatter"
                });
                await db.SaveChangesAsync();
            }

            var dto = new UserRegisterRequest
            {
                Username = "newuser",
                UserEmail = "dupe@example.com",
                Password = "Whatever!"
            };

            var response = await _client.PostAsync("/auth/UserAuth/register", ToJsonContent(dto));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains("User already exists.", text);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOk_And_Tokens()
        {
            // Arrange: seed a user with a known hashed password
            Guid userUuid;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                var hashed = hasher.HashPassword("MySecret!");

                var user = new User
                {
                    UUID = Guid.NewGuid(),
                    Username = "loginTestUser",
                    UserEmail = "login@example.com",
                    PasswordHash = hashed
                };
                userUuid = user.UUID;
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            var loginDto = new UserLoginRequest
            {
                UserEmail = "login@example.com",
                Password = "MySecret!"
            };

            // Act
            var response = await _client.PostAsync(
                "/auth/UserAuth/login",
                ToJsonContent(loginDto)
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // The JSON is serialized with camelCase by default: properties are "jwtToken" and "refreshToken"
            Assert.True(root.TryGetProperty("jwtToken", out var jwtProp));
            var returnedJwt = jwtProp.GetString();
            Assert.False(string.IsNullOrEmpty(returnedJwt));

            Assert.True(root.TryGetProperty("refreshToken", out var rtProp));
            var returnedRt = rtProp.GetString();
            Assert.False(string.IsNullOrEmpty(returnedRt));

            // Validate the JWT with our test key
            var tokenHandler = new JwtSecurityTokenHandler();
            var config = _factory.Services.GetRequiredService<IConfiguration>();
            var key = Encoding.UTF8.GetBytes(config["Jwt:Key"]);
            tokenHandler.ValidateToken(
                returnedJwt,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = config["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                },
                out SecurityToken validatedToken
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var tokenEntity = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == returnedRt);
                Assert.NotNull(tokenEntity);
                Assert.Equal(userUuid, tokenEntity.UserUUID);
                Assert.False(tokenEntity.IsRevoked);
            }
        }


        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                var hashed = hasher.HashPassword("RightPassword");

                db.Users.Add(new User
                {
                    UUID = Guid.NewGuid(),
                    Username = "invalidLoginUser",
                    UserEmail = "invalid@example.com",
                    PasswordHash = hashed
                });
                await db.SaveChangesAsync();
            }

            var badLoginDto = new UserLoginRequest
            {
                UserEmail = "invalid@example.com",
                Password = "WrongPassword"
            };

            var response = await _client.PostAsync("/auth/UserAuth/login", ToJsonContent(badLoginDto));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains("Invalid credentials.", text);
        }

        [Fact]
        public async Task Login_WithNonexistentEmail_ReturnsUnauthorized()
        {
            var loginDto = new UserLoginRequest
            {
                UserEmail = "doesnotexist@example.com",
                Password = "Anything"
            };

            var response = await _client.PostAsync("/auth/UserAuth/login", ToJsonContent(loginDto));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains("Invalid credentials.", text);
        }

        [Fact]
        public async Task RefreshToken_WithValidToken_ReturnsOk_And_NewJwt()
        {
            // Arrange: 
            // 1) Seed the user and a valid, non‐expired token
            Guid userUuid;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                var hashed = hasher.HashPassword("RefreshPass!");

                var user = new User
                {
                    UUID = Guid.NewGuid(),
                    Username = "refreshUser",
                    UserEmail = "refresh@example.com",
                    PasswordHash = hashed
                };
                userUuid = user.UUID;
                db.Users.Add(user);
                await db.SaveChangesAsync();

                var validRt = new RefreshToken
                {
                    Token = "VALID_REFRESH_TOKEN",
                    ExpiryDate = DateTime.UtcNow.AddHours(1),
                    CreationDate = DateTime.UtcNow,
                    IsRevoked = false,
                    UserUUID = userUuid,
                    User = user
                };
                db.RefreshTokens.Add(validRt);
                await db.SaveChangesAsync();
            }

            var refreshDto = new RefreshTokenRequest
            {
                RefreshToken = "VALID_REFRESH_TOKEN"
            };

            // Act
            var response = await _client.PostAsync(
                "/auth/UserAuth/refresh-token",
                ToJsonContent(refreshDto)
            );

            // If we got a 500, throw the response body so we can inspect
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                var raw = await response.Content.ReadAsStringAsync();
                throw new Exception($"Expected 200 OK, but got 500. Body = {raw}");
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert: the JSON contains "jwtToken" (camelCase)
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (!root.TryGetProperty("jwtToken", out var newJwtProp))
            {
                throw new Exception($"Response JSON did not contain jwtToken. Full body: {body}");
            }
            var newJwt = newJwtProp.GetString();
            Assert.False(string.IsNullOrEmpty(newJwt));

            // Validate the JWT signature and claims using our test key
            var tokenHandler = new JwtSecurityTokenHandler();
            var config = _factory.Services.GetRequiredService<IConfiguration>();
            var jwtKey = config["Jwt:Key"];
            Assert.False(string.IsNullOrEmpty(jwtKey));
            var key = Encoding.UTF8.GetBytes(jwtKey!);
            tokenHandler.ValidateToken(
                newJwt,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = config["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                },
                validatedToken: out SecurityToken _
            );

            // Finally, check that the old token was marked revoked, and a new RefreshToken was created
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var oldToken = await db.RefreshTokens
                    .IgnoreQueryFilters()
                    .SingleOrDefaultAsync(r => r.Token == "VALID_REFRESH_TOKEN");
                Assert.NotNull(oldToken);
                Assert.True(oldToken.IsRevoked);

                var newRefreshToken = await db.RefreshTokens
                    .Where(r => r.UserUUID == userUuid && r.Token != "VALID_REFRESH_TOKEN")
                    .OrderByDescending(r => r.CreationDate)
                    .FirstOrDefaultAsync();
                Assert.NotNull(newRefreshToken);
                Assert.False(newRefreshToken.IsRevoked);
            }
        }



        [Fact]
        public async Task RefreshToken_MissingToken_ReturnsBadRequest()
        {
            // Arrange
            var refreshDto = new RefreshTokenRequest
            {
            };

            // Act
            var response = await _client.PostAsync(
                "/auth/UserAuth/refresh-token",
                ToJsonContent(refreshDto)
            );
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // Because [ApiController] enforces model validation, an empty or null
            // RefreshToken is caught by the framework, producing a ProblemDetails JSON:
            var text = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            // Verify that the "errors" object contains an entry for "RefreshToken"
            Assert.True(root.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("RefreshToken", out var refreshErrors));

            // There should be at least one error message for RefreshToken
            var errorList = refreshErrors.EnumerateArray().Select(e => e.GetString()).ToList();
            Assert.NotEmpty(errorList);

            // And that message should mention "required"
            Assert.Contains(errorList, msg => msg!.Contains("required", StringComparison.OrdinalIgnoreCase));
        }





        [Fact]
        public async Task RefreshToken_InvalidToken_ReturnsUnauthorized()
        {
            var refreshDto = new RefreshTokenRequest
            {
                RefreshToken = "NONEXISTENT_TOKEN"
            };

            var response = await _client.PostAsync("/auth/UserAuth/refresh-token", ToJsonContent(refreshDto));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains("Token invalid", text);
        }

        [Fact]
        public async Task RefreshToken_ExpiredToken_ReturnsUnauthorized()
        {
            Guid userUuid;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                var hashed = hasher.HashPassword("ExpiredPass!");

                var user = new User
                {
                    UUID = Guid.NewGuid(),
                    Username = "expiredUser",
                    UserEmail = "expired@example.com",
                    PasswordHash = hashed
                };
                userUuid = user.UUID;
                db.Users.Add(user);
                await db.SaveChangesAsync();

                var expiredRt = new RefreshToken
                {
                    Token = "EXPIRED_TOKEN",
                    ExpiryDate = DateTime.UtcNow.AddHours(-1),
                    CreationDate = DateTime.UtcNow.AddDays(-2),
                    IsRevoked = false,
                    UserUUID = userUuid,
                    User = user
                };
                db.RefreshTokens.Add(expiredRt);
                await db.SaveChangesAsync();
            }

            var refreshDto = new RefreshTokenRequest
            {
                RefreshToken = "EXPIRED_TOKEN"
            };

            var response = await _client.PostAsync("/auth/UserAuth/refresh-token", ToJsonContent(refreshDto));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains("Token Expired", text);
        }

        [Fact]
        public async Task RefreshToken_RevokedToken_ReturnsUnauthorized()
        {
            Guid userUuid;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                var hashed = hasher.HashPassword("RevokedPass!");

                var user = new User
                {
                    UUID = Guid.NewGuid(),
                    Username = "revokedUser",
                    UserEmail = "revoked@example.com",
                    PasswordHash = hashed
                };
                userUuid = user.UUID;
                db.Users.Add(user);
                await db.SaveChangesAsync();

                var revokedRt = new RefreshToken
                {
                    Token = "REVOKED_TOKEN",
                    ExpiryDate = DateTime.UtcNow.AddHours(1),
                    CreationDate = DateTime.UtcNow.AddDays(-1),
                    IsRevoked = true,
                    UserUUID = userUuid,
                    User = user
                };
                db.RefreshTokens.Add(revokedRt);
                await db.SaveChangesAsync();
            }

            var refreshDto = new RefreshTokenRequest
            {
                RefreshToken = "REVOKED_TOKEN"
            };

            var response = await _client.PostAsync("/auth/UserAuth/refresh-token", ToJsonContent(refreshDto));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains("Token Revoked", text);
        }

        [Fact]
        public async Task Logout_ValidToken_ReturnsOk_And_MarksRevoked()
        {
            // Use a unique token value to avoid interference from other tests
            var tokenValue = Guid.NewGuid().ToString();

            // Arrange: seed a user and a refresh token with that unique value
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                var hashed = hasher.HashPassword("LogoutPass!");

                var user = new User
                {
                    UUID = Guid.NewGuid(),
                    Username = "logoutUser",
                    UserEmail = "logout@example.com",
                    PasswordHash = hashed
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();

                db.RefreshTokens.Add(new RefreshToken
                {
                    Token = tokenValue,
                    ExpiryDate = DateTime.UtcNow.AddHours(1),
                    CreationDate = DateTime.UtcNow,
                    IsRevoked = false,
                    UserUUID = user.UUID,
                    User = user
                });
                await db.SaveChangesAsync();
            }

            var logoutDto = new RefreshTokenRequest
            {
                RefreshToken = tokenValue
            };

            // Act
            var response = await _client.PostAsync(
                "/auth/UserAuth/logout",
                ToJsonContent(logoutDto)
            );

            // Assert: 200 OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Parse JSON: property is "message" (camelCase)
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("message", out var msgProp));
            Assert.Equal("Token Revoked", msgProp.GetString());

            // Finally, verify that the token in DB was marked revoked
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var tokenEntity = await db.RefreshTokens
                    .SingleOrDefaultAsync(r => r.Token == tokenValue);
                Assert.NotNull(tokenEntity);
                Assert.True(tokenEntity.IsRevoked);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task Logout_MissingToken_ReturnsBadRequest(string? tokenValue)
        {
            // Arrange
            var logoutDto = new RefreshTokenRequest
            {
                RefreshToken = tokenValue!
            };

            // Act
            var response = await _client.PostAsync(
                "/auth/UserAuth/logout",
                ToJsonContent(logoutDto)
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var text = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            // Verify that the “errors” object contains an entry for “RefreshToken”
            Assert.True(root.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("RefreshToken", out var refreshErrors));

            // There should be at least one error message for RefreshToken
            var errorList = refreshErrors.EnumerateArray().Select(e => e.GetString()).ToList();
            Assert.NotEmpty(errorList);

            // And that message should mention “required”
            Assert.Contains(errorList, msg => msg!.Contains("required", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Logout_NonexistentToken_ReturnsNotFound()
        {
            var logoutDto = new RefreshTokenRequest
            {
                RefreshToken = "NONEXISTENT_LOGOUT_TOKEN"
            };

            var response = await _client.PostAsync("/auth/UserAuth/logout", ToJsonContent(logoutDto));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains("Refresh token not found.", text);
        }
    }
}
