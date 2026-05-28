using System.Net.Http.Json;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public class UserSettingsClient(HttpClient http) : IUserSettingsClient
{
    public Task<UserSettingsDto?> GetAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<UserSettingsDto>("api/user-settings", ct);

    public async Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync("api/user-settings/profile", request, ct);
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
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(detail)
                ? $"Password change failed ({(int)response.StatusCode})."
                : detail);
        }
    }

    public async Task DeleteAccountAsync(CancellationToken ct = default)
    {
        var response = await http.DeleteAsync("api/user-settings", ct);
        response.EnsureSuccessStatusCode();
    }
}