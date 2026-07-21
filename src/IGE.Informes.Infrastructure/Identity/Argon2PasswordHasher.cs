using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace IGE.Informes.Infrastructure.Identity;

/// <summary>
/// Reemplaza el PBKDF2 por defecto de Identity por Argon2id, exigido en
/// docs/06-seguridad-amenazas.md para credenciales de usuario. El hash
/// persistido lleva un prefijo de versión ("$argon2id$v1$") para poder
/// introducir otro algoritmo/parámetros más adelante sin invalidar los
/// hashes ya guardados.
/// </summary>
public sealed class Argon2PasswordHasher : IPasswordHasher<ApplicationUser>
{
    private const string VersionPrefix = "$argon2id$v1$";

    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DegreeOfParallelism = 4;
    private const int Iterations = 4;
    private const int MemorySizeKb = 65536;

    public string HashPassword(ApplicationUser user, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt);

        return VersionPrefix
            + Convert.ToBase64String(salt) + "$"
            + Convert.ToBase64String(hash);
    }

    public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
        if (!hashedPassword.StartsWith(VersionPrefix, StringComparison.Ordinal))
        {
            return PasswordVerificationResult.Failed;
        }

        var parts = hashedPassword[VersionPrefix.Length..].Split('$');
        if (parts.Length != 2)
        {
            return PasswordVerificationResult.Failed;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = ComputeHash(providedPassword, salt);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash)
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;
    }

    private static byte[] ComputeHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySizeKb,
        };

        return argon2.GetBytes(HashSize);
    }
}
