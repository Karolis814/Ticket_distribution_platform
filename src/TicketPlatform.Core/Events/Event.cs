using TicketPlatform.Core.Common;
using TicketPlatform.Core.Tickets;
using TicketPlatform.Core.Users;
namespace TicketPlatform.Core.Events;

public class Event : BaseEntity
{
    public Guid HostId { get; set; }

    public User Host {get; set;}

    public string Title { get; set; }
    public string Description { get; set; }

    public string Location { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    // TICKET COUNT
    public int TicketCount { get; set; }
    public ICollection<Ticket> Tickets { get; set; }
}
