using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface IJWTService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId);
    Guid? ValidateRefreshToken(string token);
}