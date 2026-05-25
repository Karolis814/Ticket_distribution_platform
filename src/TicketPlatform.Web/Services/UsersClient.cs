using System.Net.Http.Json;
using System.Security.Claims;
using TicketPlatform.Shared.Dtos;


namespace TicketPlatform.Web.Services;

public class UsersClient(HttpClient http) : IUsersClient
{
    // Fallback until authentication is implemented
    private static readonly Guid FallbackUserId = Guid.Parse("b4ec49e3-31f9-4961-b2f7-3fa11aa7e7be");

    public async Task<Guid> GetCurrentUserIdAsync(CancellationToken ct = default)
    {
        try
        {
            // arghhhh 
        
            var response = await http.GetAsync("api/auth/me", ct);
            if (!response.IsSuccessStatusCode)
                return FallbackUserId;
            var user = await response.Content.ReadFromJsonAsync<WhoAmIDTO>(cancellationToken: ct);
            Guid userId;
           if ( Guid.TryParse(user?.Id.ToString(), out userId))
            {
                // Console.WriteLine($"Current user ID: {user}"); //debugging
                //var user = await response.Content.ReadFromJsonAsync<WhoAmIDTO>(cancellationToken: ct);
                if (user is not null)
                return userId;
            }
            return  FallbackUserId;
        }
        catch
        {
            return FallbackUserId;
        }
    }

    private record UserResponse(Guid Id);
}
