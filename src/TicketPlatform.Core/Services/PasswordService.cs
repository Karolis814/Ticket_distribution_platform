using System.Security.Cryptography;
using TicketPlatform.Core.Services;

namespace TicketPlatform.Core.Services;

public class PasswordService : IPasswordService
{
    private const int SaltSize       = 32;   
    private const int HashSize       = 32;   // bit size
    private const int Iterations     = 310_000; 
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string GenerateSalt()
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        return Convert.ToBase64String(saltBytes);
    }

    public string HashPassword(string password, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);

        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password.AsSpan(),          
            saltBytes,
            Iterations,
            Algorithm,
            HashSize
        );

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string salt, string expectedHash)
    {
        string actualHash = HashPassword(password, salt);

     
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(actualHash),
            Convert.FromBase64String(expectedHash)
        );
    }
}