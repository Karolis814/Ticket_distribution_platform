using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Core.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int AmountCents { get; set; }
    public required string Currency { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public string? StripeStatus { get; set; }
    public DateTimeOffset? SucceededAt { get; set; }
    public string? StripeInvoiceId { get; set; }
    public string? StripeInvoiceUrl { get; set; }
    public string? StripeInvoicePdfUrl { get; set; }
}
