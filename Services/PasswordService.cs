using System.Security.Cryptography;
using System.Text;

namespace MLYSO.Web.Services;

public sealed class PasswordService
{
    private const int Iterations = 120_000;
    private const int SaltSize = 16;   // bytes
    private const int KeySize = 32;   // bytes
    private const string Prefix = "pbkdf2$"; // yeni format: pbkdf2$<iter>$<saltB64>$<keyB64>

    public string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        Span<byte> salt = stackalloc byte[SaltSize];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt.ToArray(), Iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize);
        return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    // yeni imza
    public bool Verify(string password, string? stored, out string? upgradedHash)
    {
        upgradedHash = null;
        if (string.IsNullOrWhiteSpace(stored)) return false;

        // Yeni format: pbkdf2$
        if (stored.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4) return false;

            var iter = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var key = Convert.FromBase64String(parts[3]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iter, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(key.Length);

            var ok = CryptographicOperations.FixedTimeEquals(computed, key);
            if (ok && iter < Iterations) // daha düþük iter ise upgrade et
                upgradedHash = HashPassword(password);

            return ok;
        }

        // --- Eski (legacy) olasýlýk 1: düz SHA256 HEX (64 hane) ---
        if (stored.Length == 64 && stored.All(IsHex))
        {
            using var sha = SHA256.Create();
            var hex = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();
            var ok = string.Equals(hex, stored, StringComparison.OrdinalIgnoreCase);
            if (ok) upgradedHash = HashPassword(password);
            return ok;
        }

        // --- Eski olasýlýk 2: SHA256 BASE64 (FormatException’ý buradan alýyordun) ---
        try
        {
            using var sha = SHA256.Create();
            var b64 = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
            var ok = string.Equals(b64, stored, StringComparison.Ordinal);
            if (ok) upgradedHash = HashPassword(password);
            return ok;
        }
        catch
        {
            return false;
        }
    }

    public bool IsModernHash(string? stored)
        => !string.IsNullOrEmpty(stored) && stored.StartsWith(Prefix, StringComparison.Ordinal);

    private static bool IsHex(char c)
        => (c is >= '0' and <= '9') || (c is >= 'a' and <= 'f') || (c is >= 'A' and <= 'F');
}
