using TicketPlatform.Core.Common;
using TicketPlatform.Core.Orders;
using TicketPlatform.Core.Users;

namespace TicketPlatform.Core.Customers;
public class Customer : BaseEntity
{

    public Guid? UserId { get; set; }
    public User User { get; set; }

    public string Email { get; set; }

    public ICollection<Order> Orders { get; set; }
}