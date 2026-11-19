
using System;
using System.Security.Cryptography;

namespace SmartBook.Helpers
{
    public class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        // Tao mat khau bam PBKDF2
        public static string HashPassword(string password)
        {
            byte[] salt = new byte[SaltSize];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, Iterations))
            {
                byte[] hash = pbkdf2.GetBytes(HashSize);

                return Convert.ToBase64String(salt)
                    + ":"
                    + Convert.ToBase64String(hash);
            }
        }

        // Kiem tra mat khau
        public static bool VerifyPassword(
            string password,
            string storedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(storedPassword))
                return false;

            string[] parts = storedPassword.Split(':');

            if (parts.Length != 2)
                return false;

            try
            {
                byte[] salt =
                    Convert.FromBase64String(parts[0]);

                byte[] originalHash =
                    Convert.FromBase64String(parts[1]);

                using (var pbkdf2 = new Rfc2898DeriveBytes(
                    password, salt, Iterations))
                {
                    byte[] newHash =
                        pbkdf2.GetBytes(originalHash.Length);

                    if (newHash.Length != originalHash.Length)
                        return false;

                    int difference = 0;

                    for (int i = 0; i < newHash.Length; i++)
                    {
                        difference |= newHash[i] ^ originalHash[i];
                    }

                    return difference == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
