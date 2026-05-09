namespace TicketPlatform.Core.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; }

    public int PriceAtPurchase { get; set; }
}
