using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public interface IUserSettingsClient
{
    Task<UserSettingsDto?> GetAsync(Guid userId, CancellationToken ct = default);
    Task ChangeEmailAsync(ChangeEmailRequest request, CancellationToken ct = default);
    Task ConfirmEmailAsync(ConfirmEmailChangeRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
    Task UpdateEmailRemindersAsync(UpdateEmailRemindersRequest request, CancellationToken ct = default);
}
