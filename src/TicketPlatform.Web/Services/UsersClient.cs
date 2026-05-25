using System.Net.Http.Json;

namespace TicketPlatform.Web.Services;

public class UsersClient(HttpClient http) : IUsersClient
{
    // Fallback until authentication is implemented
    private static readonly Guid FallbackUserId = Guid.Parse("b4ec49e3-31f9-4961-b2f7-3fa11aa7e7be");

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
