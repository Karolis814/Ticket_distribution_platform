using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Core.Entities;

public class Order : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int TotalPriceCents { get; set; }
    public required string Currency { get; set; }
    public OrderStatus Status { get; set; }

    public Payment? Payment { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
