namespace TicketPlatform.Core.Entities;

public class User : BaseEntity
{
    public Guid UserPermissionGroupId { get; set; }
    public UserPermissionGroup UserPermissionGroup { get; set; } = null!;

    public string? Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string PasswordSalt { get; set; }
    public string? Company { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? PhoneNumber { get; set; }

    public ICollection<Event> HostedEvents { get; set; } = new List<Event>();

    public bool EmailConfirmed { get; set; } = true;
    public string? PendingEmail { get; set; }
    public string? EmailConfirmationTokenHash { get; set; }
    public DateTimeOffset? EmailConfirmationTokenExpiresAt { get; set; }
    public bool EmailRemindersEnabled { get; set; } = true;

}
