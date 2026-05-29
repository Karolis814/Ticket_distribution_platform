using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Settings;

namespace TicketPlatform.Core.Services;

public class JWTService(IOptions<JWTSettings> options) : IJWTService
{
    private readonly JWTSettings _settings = options.Value;

    public int AccessTokenExpiryMinutes => _settings.AccessTokenExpiryMinutes;

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = BuildClaims(user);

        var token = new JwtSecurityToken(
            issuer:             _settings.Issuer,
            audience:           _settings.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(Guid userId)
    {
        return new RefreshToken
        {
            UserId    = userId,
            Token     = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpiryDays),
            IsRevoked = false
        };
    }

    private static List<Claim> BuildClaims(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("role",                             user.Role.ToString()),
        };

        return claims;
    }
}