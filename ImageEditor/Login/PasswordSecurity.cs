using System;

namespace ImageEditor.Login
{
    internal static class PasswordSecurity
    {
        public static string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool Verify(
            string inputPassword,
            string storedPassword,
            out bool needsUpgrade)
        {
            needsUpgrade = false;

            if (string.IsNullOrEmpty(storedPassword))
                return false;

            if (LooksLikeBcryptHash(storedPassword))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(inputPassword, storedPassword);
                }
                catch
                {
                    return false;
                }
            }

            // 이전 버전이 평문으로 저장한 계정도 한 번은 로그인할 수 있게 하고,
            // 성공 직후 BCrypt 해시로 자동 전환한다.
            bool matches = string.Equals(
                inputPassword,
                storedPassword,
                StringComparison.Ordinal);
            needsUpgrade = matches;
            return matches;
        }

        private static bool LooksLikeBcryptHash(string value)
        {
            return value.StartsWith("$2a$", StringComparison.Ordinal) ||
                   value.StartsWith("$2b$", StringComparison.Ordinal) ||
                   value.StartsWith("$2y$", StringComparison.Ordinal);
        }
    }
}
