using System.Security.Claims;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface IJWTService
{
    int AccessTokenExpiryMinutes { get; }
    string GenerateAccessToken(User user);
    string RefreshAccessToken(ClaimsPrincipal principal);
    RefreshToken GenerateRefreshToken(Guid userId);
    Guid? ValidateRefreshToken(string token);
}