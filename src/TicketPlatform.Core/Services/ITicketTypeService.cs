using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface ITicketTypeService
{
    Task<TicketType?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TicketType> CreateAsync(TicketType ticketType, CancellationToken ct = default);
    Task ReserveAsync(Guid ticketTypeId, int quantity, CancellationToken ct = default);
}
