using TicketPlatform.Core.Common;
using TicketPlatform.Core.Events;
using TicketPlatform.Core.OrderItems;


namespace TicketPlatform.Core.Tickets;
public class Ticket
{
    public Guid TicketId { get; set; }

    public Guid EventId { get; set; }
    public Event Event { get; set; }

    public int Price { get; set; } // stored in cents
    public string Currency { get; set; }

    public int? SeatNumber { get; set; }

    public string Status { get; set; } // available, reserved, sold

    public ICollection<OrderItem> OrderItems { get; set; }
}