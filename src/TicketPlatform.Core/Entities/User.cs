namespace TicketPlatform.Core.Entities;

public class User : BaseEntity
{
    public Guid UserPermissionGroupId { get; set; }

    public UserPermissionGroup UserPermissionGroup { get; set; }

    public string UserName { get; set; }
    public required string Email { get; set; }

    public ICollection<Event> HostedEvents { get; set; }
}
