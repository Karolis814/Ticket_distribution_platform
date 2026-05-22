using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public class UserPermissionGroupService(IRepository<UserPermissionGroup> repository) : IUserPermissionGroupService
{
   

    public async Task<UserPermissionGroup> GetByTitleAsync(string title, CancellationToken ct = default)
    {
         UserPermissionGroup? grp;
         if (title == null)
            throw new ArgumentNullException(nameof(title));
        try
        {

            grp = await repository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(upg => upg.Title == title, ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("An error occurred while retrieving the user permission group. Please try again.", ex);
        }

        if (grp == null)
        {
            throw new KeyNotFoundException($"User permission group with title '{title}' not found.");
        }

        return grp;
    }
    public async Task<UserPermissionGroup> CreateAsync(UserPermissionGroup entity, CancellationToken ct = default)
    {
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return entity;
    }

    public Task<UserPermissionGroup?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return repository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<UserPermissionGroup> UpdateAsync(UserPermissionGroup entity, CancellationToken ct = default)
    {
        try
       {
            repository.Update(entity);
            await repository.SaveChangesAsync(ct);
            return entity;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("An error occurred while updating the user permission group. Please try again.", ex);

        }
        
    }
}