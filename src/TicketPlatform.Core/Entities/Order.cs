using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Core.Entities;

public class Order : BaseEntity
{
    public Guid CustomerId { get; set; }

    // create Customer when setting up the order, delete on failed Order completion,
    public Customer? Customer { get; set; }

    public int TotalPrice { get; set; }
    public string Currency { get; set; }

    public OrderStatus Status { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public Payment Payment { get; set; }
}
