using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface IJWTService
{
    int AccessTokenExpiryMinutes { get; }
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId);
}