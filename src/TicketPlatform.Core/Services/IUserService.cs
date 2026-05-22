using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Core.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User> CreateAsync(UserSignUpDTO entity, CancellationToken ct = default);
    Task<User> UpdateAsync(User entity, CancellationToken ct = default);

}