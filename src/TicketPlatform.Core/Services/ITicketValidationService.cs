using TicketPlatform.Core.Models;

namespace TicketPlatform.Core.Services;

public interface ITicketValidationService
{
    Task<TicketValidationResult> ValidateAsync(Guid ticketId, Guid userId, CancellationToken ct = default);
}
