namespace TicketPlatform.Core.Settings;

public class JWTSettings
{
    public required string Secret { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int AccessTokenExpiryMinutes { get; set; }
}