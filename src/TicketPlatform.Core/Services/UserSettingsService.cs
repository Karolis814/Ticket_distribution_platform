using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Mail.Templates;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Core.Services;

public class UserSettingsService(
    IRepository<User> userRepository,
    IMailService mail) : IUserSettingsService
{
    public async Task<UserSettingsDto?> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, ct);

        return user is null
            ? null
            : new UserSettingsDto(
                user.Id,
                user.Email,
                user.EmailConfirmed,
                user.PendingEmail,
                user.EmailRemindersEnabled);
    }

    public async Task RequestEmailChangeAsync(
        Guid userId,
        string newEmail,
        string confirmationBaseUrl,
        CancellationToken ct = default)
    {
        var user = await userRepository.Query()
            .FirstOrDefaultAsync(x => x.Id == userId, ct);

        if (user is null)
            throw new InvalidOperationException("User not found.");

        var emailExists = await userRepository.Query()
            .AnyAsync(x => x.Email == newEmail && x.Id != userId, ct);

        if (emailExists)
            throw new InvalidOperationException("Email is already used.");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(token);

        user.PendingEmail = newEmail;
        user.EmailConfirmed = false;
        user.EmailConfirmationTokenHash = tokenHash;
        user.EmailConfirmationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(24);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        var confirmationUrl =
            $"{confirmationBaseUrl.TrimEnd('/')}/settings/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

        await mail.SendAsync(EmailTemplates.ConfirmEmailChange(
            toEmail: newEmail,
            toName: user.Username ?? user.Email,
            confirmationUrl: confirmationUrl), ct);
    }

    public async Task ConfirmEmailChangeAsync(
        Guid userId,
        string token,
        CancellationToken ct = default)
    {
        var user = await userRepository.Query()
            .FirstOrDefaultAsync(x => x.Id == userId, ct);

        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (string.IsNullOrWhiteSpace(user.PendingEmail))
            throw new InvalidOperationException("No pending email change.");

        if (user.EmailConfirmationTokenExpiresAt < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Email confirmation token expired.");

        if (user.EmailConfirmationTokenHash != HashToken(token))
            throw new InvalidOperationException("Invalid email confirmation token.");

        user.Email = user.PendingEmail;
        user.PendingEmail = null;
        user.EmailConfirmed = true;
        user.EmailConfirmationTokenHash = null;
        user.EmailConfirmationTokenExpiresAt = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await userRepository.Query()
            .FirstOrDefaultAsync(x => x.Id == userId, ct);

        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (!VerifyPassword(currentPassword, user.PasswordSalt, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var hash = HashPassword(newPassword, salt);

        user.PasswordSalt = salt;
        user.PasswordHash = hash;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);
    }

    public async Task UpdateEmailRemindersAsync(
        Guid userId,
        bool enabled,
        CancellationToken ct = default)
    {
        var user = await userRepository.Query()
            .FirstOrDefaultAsync(x => x.Id == userId, ct);

        if (user is null)
            throw new InvalidOperationException("User not found.");

        user.EmailRemindersEnabled = enabled;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private static string HashPassword(string password, string salt)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + salt));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string salt, string hash)
    {
        return HashPassword(password, salt) == hash;
    }
}
