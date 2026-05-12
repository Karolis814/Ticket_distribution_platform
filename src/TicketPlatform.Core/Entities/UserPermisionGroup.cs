using System.ComponentModel.DataAnnotations;

namespace TicketPlatform.Core.Entities;

public class UserPermissionGroup : BaseEntity
{
    public required string Title { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
