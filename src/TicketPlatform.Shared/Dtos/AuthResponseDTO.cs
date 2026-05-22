namespace TicketPlatform.Shared.Dtos;

public class AuthResponseDTO
{
    public required string AccessToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public required string Email { get; set; }
    public string? Username { get; set; }
    public required string PermissionGroup { get; set; }
}