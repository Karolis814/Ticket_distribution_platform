namespace TicketPlatform.Core.Entities;

public class Ticket : BaseEntity
{
    public Guid TicketTypeId { get; set; }
    public TicketType TicketType { get; set; } = null!;

    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;

    public int TimesUsed { get; set; }
}
