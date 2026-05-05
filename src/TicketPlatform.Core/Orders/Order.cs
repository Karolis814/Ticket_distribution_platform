using TicketPlatform.Core.Customers;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.OrderItems;
using TicketPlatform.Core.Payments;

namespace TicketPlatform.Core.Orders;
public class Order : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; }

    public int TotalPrice { get; set; }
    public string Currency { get; set; }

    public string OrderStatus { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; }
    
    public ICollection<Payment> Payments { get; set; }
}