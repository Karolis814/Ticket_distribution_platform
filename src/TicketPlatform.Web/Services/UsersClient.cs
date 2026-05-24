using System.Net.Http.Json;

namespace TicketPlatform.Web.Services;

public class UsersClient(HttpClient http) : IUsersClient
{
    // Fallback until authentication is implemented
    private static readonly Guid FallbackUserId = Guid.Parse("8dc55ac3-5e02-49fb-867e-7aa82d3ca8bc");

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