using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Core.Services;

public class UserService(IRepository<User> repository, IPasswordService passwordService, IUserPermissionGroupService   permissionGroupService) : IUserService
{
    //private readonly UserPermissionGroupService permissionGroupService = new UserPermissionGroupService(permissionGroupRepository);
    public async Task<User> CreateAsync(UserSignUpDTO entity, CancellationToken ct = default)
    {
        var salt = passwordService.GenerateSalt();
        var permissionGroup = await permissionGroupService.GetByTitleAsync(entity.role, ct);

        
        User user = new User
        {
            UserPermissionGroupId = permissionGroup.Id,
            Id = Guid.NewGuid(),
            Username = entity.Name,
            Email = entity.Email,
            PasswordSalt = salt,
            PasswordHash = passwordService.HashPassword(entity.Password, salt),
            Company = entity.Company,
            Address = entity.Address,
            TaxCode = entity.TaxCode,
            PhoneNumber = entity.PhoneNumber
        };      
        await repository.AddAsync(user, ct);
        try        {

        await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("An error occurred while creating the user. Please try again.", ex);
        }
        finally
        {
           
            user.PasswordHash = null!;
            user.PasswordSalt = null!;
            
        }
        return user;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return repository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public Task<User> UpdateAsync(User entity, CancellationToken ct = default)
    {
        try
       {
            try            {
                var existing = repository.Query().FirstOrDefault(u => u.Id == entity.Id);
                if (existing == null)
                {
                    throw new InvalidOperationException("User not found.");
                }

            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("An error occurred while updating the user. Please try again.", ex);
            }
           repository.Update(entity);
            return repository.SaveChangesAsync(ct)
                .ContinueWith(_ => entity, ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("The user was modified by another process. Please reload and try again.", ex);
        }
         catch (InvalidOperationException ex)
        {
            return Task.FromException<User>(ex);
        }
    }

    
}