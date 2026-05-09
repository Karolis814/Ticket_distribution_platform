namespace TicketPlatform.Core.Entities;

public class UserPermissionGroup : BaseEntity
{
    public required Boolean IsActive { get; set; }

    public required ICollection<Permission> Permissions { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
