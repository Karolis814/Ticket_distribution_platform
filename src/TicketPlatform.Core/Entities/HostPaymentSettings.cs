namespace TicketPlatform.Core.Entities;

public class HostPaymentSettings : BaseEntity
{
    public Guid HostId { get; set; }
    public User Host { get; set; } = null!;

    public string? StripeAccountId { get; set; }

    public bool ChargesEnabled { get; set; }
    public bool PayoutsEnabled { get; set; }
    public bool DetailsSubmitted { get; set; }

    public DateTimeOffset? OnboardedAt { get; set; }
}
