using System.ComponentModel.DataAnnotations;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Core.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid TicketTypeId { get; set; }
    public TicketType TicketType { get; set; } = null!;

    public int Quantity { get; set; }
    public int UnitPriceCents { get; set; } // at the time of purchase
    public required string Currency { get; set; }

    public ReminderStatus ReminderStatus { get; set; } = ReminderStatus.Pending;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
