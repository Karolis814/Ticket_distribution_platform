namespace TicketPlatform.Core.Entities;

public class PasswordResetToken : BaseEntity
{
    public required Guid UserId { get; set; }

    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }
}