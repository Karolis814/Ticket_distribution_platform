namespace TicketPlatform.Core.Entities;

public class Category : BaseEntity
{
    public required string Title { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
