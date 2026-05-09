namespace TicketPlatform.Core.Entities;

public class Permission : BaseEntity
{
    public required String Title { get; set; }

    public required Boolean PermissionStatus { get; set; }

    public ICollection<UserPermissionGroup> UserPermissionGroups { get; set; }
        = new List<UserPermissionGroup>();
}
