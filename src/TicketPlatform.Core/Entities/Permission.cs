namespace TicketPlatform.Core.Entities;

public class Permission : BaseEntity
{
    public required string Title { get; set; }

    public ICollection<UserPermissionGroup> UserPermissionGroups { get; set; } = new List<UserPermissionGroup>();
}
