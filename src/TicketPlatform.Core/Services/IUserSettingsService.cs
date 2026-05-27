using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Core.Services;

public interface IUserSettingsService
{
    Task<UserSettingsDto?> GetAsync(Guid userId, CancellationToken ct = default);

    Task RequestEmailChangeAsync(
        Guid userId,
        string newEmail,
        string confirmationBaseUrl,
        CancellationToken ct = default);

    Task ConfirmEmailChangeAsync(
        Guid userId,
        string token,
        CancellationToken ct = default);

    Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default);

    Task UpdateEmailRemindersAsync(
        Guid userId,
        bool enabled,
        CancellationToken ct = default);
}
