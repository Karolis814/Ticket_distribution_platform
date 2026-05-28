
namespace TicketPlatform.Shared.Dtos;
public record ResetPasswordRequest
{
    public required string Email { get; init; }
    public  string Token { get; init; }
    public string NewPassword { get; init; }
}