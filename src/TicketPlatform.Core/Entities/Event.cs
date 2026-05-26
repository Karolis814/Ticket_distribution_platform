using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Core.Entities;

public class Event : BaseEntity
{
    public Guid HostId { get; set; }
    public User Host { get; set; } = null!;

    public EventCategory Category { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string? Location { get; set; }
    public string? ThumbnailUrl { get; set; }
    public EventStatus Status { get; set; }

    public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
}
