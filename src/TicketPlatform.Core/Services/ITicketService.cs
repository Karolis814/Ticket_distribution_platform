using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface ITicketService
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Ticket> CreateAsync(Ticket entity, CancellationToken ct = default);
    Task<Ticket> UpdateAsync(Ticket entity, CancellationToken ct = default);
}
