using TicketPlatform.Core.Common;
using TicketPlatform.Core.Orders;
using TicketPlatform.Core.Tickets;

namespace TicketPlatform.Core.OrderItems;
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; }

    public int PriceAtPurchase { get; set; }
}