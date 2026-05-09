namespace TicketPlatform.Core.Entities;

public class Payment
{
    public Guid PaymentId { get; set; }

    public Guid OrderId { get; set; }
    public Order Order { get; set; }

    public string StripePaymentIntentId { get; set; }
    public string StripeCheckoutSessionId { get; set; }

    public int Amount { get; set; } // centai i.e 100 -> 1.00 eur
    public string Currency { get; set; }

    public string Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SucceededAt { get; set; }
}
