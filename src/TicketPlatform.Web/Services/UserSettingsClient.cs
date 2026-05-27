using System.Net.Http.Json;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public class UserSettingsClient(HttpClient http) : IUserSettingsClient
{
    public Task<UserSettingsDto?> GetAsync(Guid userId, CancellationToken ct = default)
        => http.GetFromJsonAsync<UserSettingsDto>($"api/user-settings/{userId}", ct);

    public async Task ChangeEmailAsync(ChangeEmailRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/user-settings/change-email", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ConfirmEmailAsync(ConfirmEmailChangeRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/user-settings/confirm-email", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/user-settings/change-password", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateEmailRemindersAsync(UpdateEmailRemindersRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/user-settings/email-reminders", request, ct);
        response.EnsureSuccessStatusCode();
    }
}
