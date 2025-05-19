using Auth.API.Interfaces;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace Auth.API.Logic
{
    public class SHA256PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 122365;
        private static readonly KeyDerivationPrf Algorithm = KeyDerivationPrf.HMACSHA256;

        public string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: Algorithm,
                iterationCount: Iterations,
                numBytesRequested: HashSize
            );

            return $"{Convert.ToHexString(hash)}.{Convert.ToHexString(salt)}";
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            var parts = hashedPassword.Split('.');
            if (parts.Length != 2)
                return false;

            byte[] salt = Convert.FromHexString(parts[1]);
            byte[] storedHash = Convert.FromHexString(parts[0]);

            byte[] computedHash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: Algorithm,
                iterationCount: Iterations,
                numBytesRequested: HashSize
            );
            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
    }
}
