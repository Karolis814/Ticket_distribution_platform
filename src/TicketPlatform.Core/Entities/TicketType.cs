namespace TicketPlatform.Core.Entities;

public class TicketType : BaseEntity
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public required string Title { get; set; }
    public DateTimeOffset OccurenceStartDate { get; set; }
    public DateTimeOffset OccurenceEndDate { get; set; }
    public DateTimeOffset AdmissionStartDate { get; set; }
    public DateTimeOffset AdmissionEndDate { get; set; }
    public int PriceCents { get; set; }
    public required string Currency { get; set; }
    public int MaxUses { get; set; } = 1;
    public int Quantity { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
