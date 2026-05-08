
using System.Net;
using TicketPlatform.Core.Common;

namespace TicketPlatform.Core.Users;

public class Permission : BaseEntity
{
    
    public required String Title {get; set;}

    public required Boolean PermissionStatus {get; set;} 

    public ICollection<UserPermissionGroup> UserPermissionGroups { get; set; }
        = new List<UserPermissionGroup>();
}