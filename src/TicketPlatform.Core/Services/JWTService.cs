using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;
using TicketPlatform.Core.Settings;

namespace TicketPlatform.Core.Services;

public class JWTService(IOptions<JWTSettings> options) : IJWTService
{
    private readonly JWTSettings _settings = options.Value;

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);


        //cia gyvena visi permisions in refresh token
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

    // refresh token WIP
    public Guid? ValidateRefreshToken(string token)
    {
        
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return Guid.Empty; 
    }

   

    private static List<Claim> BuildClaims(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),  
            new("permission_group",            user.UserPermissionGroup.Title),
        };
        
        foreach (var permission in user.UserPermissionGroup.Permissions)
        {
           claims.Add(new Claim("permission", permission.Title));
        }

        return claims;
    }
}