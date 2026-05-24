using System.Net.Http.Json;

namespace TicketPlatform.Web.Services;

public class UsersClient(HttpClient http) : IUsersClient
{
    // Fallback until authentication is implemented
    private static readonly Guid FallbackUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task<Guid> GetCurrentUserIdAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync("api/users/me", ct);
            if (!response.IsSuccessStatusCode)
                return FallbackUserId;

            var user = await response.Content.ReadFromJsonAsync<UserResponse>(cancellationToken: ct);
            return user?.Id is { } id && id != Guid.Empty ? id : FallbackUserId;
        }
        catch
        {
            return FallbackUserId;
        }
    }

    private record UserResponse(Guid Id);
}
