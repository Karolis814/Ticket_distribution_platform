using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Core.Entities;

public class Ticket : BaseEntity
{
    public Guid EventId { get; set; }
    public Event Event { get; set; }

    public int Price { get; set; } // stored in cents
    public string Currency { get; set; }


    public int? SeatNumber { get; set; }
    // start end timestamp
    //int activasionCount

    public DateTimeOffset admisionStart { get; set; }

    public DateTimeOffset admisionEnd { get; set; }
    public TicketStatus Status { get; set; }
}
