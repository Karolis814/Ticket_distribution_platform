using TicketPlatform.Shared.Dtos;
using TicketPlatform.Core.Entities;


public interface IPasswordResetTokenService
{
    
    Task<PasswordResetToken?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PasswordResetToken> CreateAsync(PasswordResetToken entity, CancellationToken ct = default);
    Task<PasswordResetToken> UpdateAsync(PasswordResetToken entity, CancellationToken ct = default);




}