using TicketPlatform.Core.Common;
using TicketPlatform.Core.Events;


namespace TicketPlatform.Core.Users;
public class User
{
    public Guid UserId { get; set; }

    public Guid UserPermissionGroupId { get; set; }
    // for now not using 
    //public UserPermissionGroup UserPermissionGroup { get; set; }

    public string Name { get; set; }
    public string Surname { get; set; }
    public string Email { get; set; }

    public ICollection<Event> HostedEvents { get; set; }
}