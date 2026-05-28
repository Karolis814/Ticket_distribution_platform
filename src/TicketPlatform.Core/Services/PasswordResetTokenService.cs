using TicketPlatform.Core.Common;
using System.Security.Cryptography;
using System.Text;
using TicketPlatform.Core.Entities;

public class PasswordResetTokenService(IRepository<PasswordResetToken> repository) : IPasswordResetTokenService
{
    

    public async Task<PasswordResetToken?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await repository.GetByIdAsync(id);
    }
    public async Task<PasswordResetToken> CreateAsync(PasswordResetToken entity, CancellationToken ct = default)
    {
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return entity;
        
    }
    public async Task<PasswordResetToken> UpdateAsync(PasswordResetToken entity, CancellationToken ct = default)
    {
        repository.Update(entity);
        await repository.SaveChangesAsync();
        return entity;

    }

    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    public static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }
    public async Task<string> CreatePasswordResetTokenAsync(User user)
    {
    var rawToken = GenerateToken();
    var hash = HashToken(rawToken);

    var entity = new PasswordResetToken
        {
        UserId = user.Id,
        TokenHash = hash,
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        };
        await repository.AddAsync(entity);
    await repository.SaveChangesAsync();

    return rawToken; 
    }

    public async Task<PasswordResetToken?> GetByUserId(Guid userId, CancellationToken ct = default)
    {
        return repository.Query()
            .FirstOrDefault(t => t.UserId == userId);
    }
}