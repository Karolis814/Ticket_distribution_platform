using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface ITicketTypeService
{
    Task<TicketType?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TicketType> CreateAsync(TicketType ticketType, CancellationToken ct = default);
    Task<bool> TryReserveAsync(Guid id, int quantity, CancellationToken ct = default);
    Task<int> GetSoldTickets(Guid id, CancellationToken ct = default);
}
