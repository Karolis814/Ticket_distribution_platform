using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Core.Entities;

public class User : BaseEntity
{
    public UserRole Role { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string PasswordSalt { get; set; }
    public string? Company { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? PhoneNumber { get; set; }

    public string? StripeAccountId { get; set; }
    public DateTimeOffset? StripeOnboardedAt { get; set; }

    public ICollection<Event> HostedEvents { get; set; } = new List<Event>();

    public bool EmailConfirmed { get; set; } = true;
    public string? PendingEmail { get; set; }
    public string? EmailConfirmationTokenHash { get; set; }
    public DateTimeOffset? EmailConfirmationTokenExpiresAt { get; set; }

}
