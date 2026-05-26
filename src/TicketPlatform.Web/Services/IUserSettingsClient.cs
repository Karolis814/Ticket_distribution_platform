using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public interface IUserSettingsClient
{
    Task<UserSettingsDto?> GetAsync(CancellationToken ct = default);
    Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task ConfirmEmailAsync(ConfirmEmailChangeRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
    Task DeleteAccountAsync(CancellationToken ct = default);
}