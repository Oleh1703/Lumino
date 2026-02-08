using System.Security.Cryptography;
using System.Text;

namespace Lumino.API.Utils
{
    public class PasswordHasher : IPasswordHasher
    {
        // Format: pbkdf2.v1.{iterations}.{saltBase64}.{subKeyBase64}
        private const string Prefix = "pbkdf2.v1";
        private const int SaltSize = 16;
        private const int SubKeySize = 32;

        private const int Iterations = 100_000;

        public string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password is required");
            }

            var salt = RandomNumberGenerator.GetBytes(SaltSize);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256
            );

            var subKey = pbkdf2.GetBytes(SubKeySize);

            return $"{Prefix}.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subKey)}";
        }

        public bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            if (storedHash.StartsWith(Prefix + ".", StringComparison.Ordinal))
            {
                return VerifyPbkdf2(password, storedHash);
            }

            // Legacy SHA256(Base64) support
            return VerifyLegacySha256(password, storedHash);
        }

        public bool NeedsRehash(string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return true;
            }

            if (!storedHash.StartsWith(Prefix + ".", StringComparison.Ordinal))
            {
                return true;
            }

            var parts = storedHash.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
            {
                return true;
            }

            if (!int.TryParse(parts[2], out var iterations))
            {
                return true;
            }

            return iterations < Iterations;
        }

        private static bool VerifyPbkdf2(string password, string storedHash)
        {
            var parts = storedHash.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
            {
                return false;
            }

            if (parts[0] != "pbkdf2" || parts[1] != "v1")
            {
                return false;
            }

            if (!int.TryParse(parts[2], out var iterations) || iterations <= 0)
            {
                return false;
            }

            byte[] salt;
            byte[] expectedSubKey;

            try
            {
                salt = Convert.FromBase64String(parts[3]);
                expectedSubKey = Convert.FromBase64String(parts[4]);
            }
            catch
            {
                return false;
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256
            );

            var actualSubKey = pbkdf2.GetBytes(expectedSubKey.Length);

            return CryptographicOperations.FixedTimeEquals(actualSubKey, expectedSubKey);
        }

        private static bool VerifyLegacySha256(string password, string storedHash)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            var base64 = Convert.ToBase64String(hash);

            var a = Encoding.UTF8.GetBytes(base64);
            var b = Encoding.UTF8.GetBytes(storedHash);

            if (a.Length != b.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
