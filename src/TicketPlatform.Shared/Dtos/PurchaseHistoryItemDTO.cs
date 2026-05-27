namespace TicketPlatform.Shared.Dtos;

public class PurchaseHistoryItemDTO
{
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = "";
    public string TicketTypeTitle { get; set; } = "";
    public int Quantity { get; set; }
    public int UnitPriceCents { get; set; }
    public int TotalPriceCents { get; set; }
    public string Currency { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTimeOffset PurchasedAt { get; set; }
    public string? InvoiceUrl { get; set; }
}
