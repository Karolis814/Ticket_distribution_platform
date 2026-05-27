namespace TicketPlatform.Core.Entities;

public class Customer : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public bool EmailRemindersEnabled { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
