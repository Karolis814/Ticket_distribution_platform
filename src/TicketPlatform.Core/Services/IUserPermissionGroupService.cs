using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface IUserPermissionGroupService
{
    Task<UserPermissionGroup> GetByTitleAsync(string title, CancellationToken ct = default);
    Task<UserPermissionGroup?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserPermissionGroup> CreateAsync(UserPermissionGroup entity, CancellationToken ct = default);
    Task<UserPermissionGroup> UpdateAsync(UserPermissionGroup entity, CancellationToken ct = default);
}