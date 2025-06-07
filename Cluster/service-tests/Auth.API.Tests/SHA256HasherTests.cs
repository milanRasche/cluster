using Auth.API.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Auth.API.Tests
{
    public class SHA256PasswordHasherTests
    {
        private readonly SHA256PasswordHasher _hasher = new();
        private const string TestPassword = "My$up3rS3cret!";

        [Fact]
        public void HashPassword_ShouldProduceDifferentHashes_ForSamePassword()
        {
            // Arrange & Act
            string hash1 = _hasher.HashPassword(TestPassword);
            string hash2 = _hasher.HashPassword(TestPassword);

            // Assert
            Assert.NotNull(hash1);
            Assert.NotNull(hash2);
            Assert.NotEqual(hash1, hash2); // salts differ
        }

        [Fact]
        public void VerifyPassword_ShouldReturnTrue_ForCorrectPasswordAndHash()
        {
            // Arrange
            string hashed = _hasher.HashPassword(TestPassword);

            // Act
            bool result = _hasher.VerifyPassword(TestPassword, hashed);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("wrongpassword")]         // wrong password
        [InlineData("")]                      // empty password
        public void VerifyPassword_ShouldReturnFalse_ForIncorrectPassword(string wrongPassword)
        {
            // Arrange
            string hashed = _hasher.HashPassword(TestPassword);

            // Act
            bool result = _hasher.VerifyPassword(wrongPassword, hashed);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("")]              // empty string
        [InlineData("not–a–valid–hash")]
        [InlineData("abc.def.ghi")]   // too many parts
        public void VerifyPassword_ShouldReturnFalse_ForMalformedHash(string malformedHash)
        {
            // Act
            bool result = _hasher.VerifyPassword(TestPassword, malformedHash);

            // Assert
            Assert.False(result);
        }
    }
}
