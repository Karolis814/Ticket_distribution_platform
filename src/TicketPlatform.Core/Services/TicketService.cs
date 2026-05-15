using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public class TicketService(IRepository<Ticket> repository) : ITicketService
{
    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await repository.Query()
            .Include(t => t.TicketType)
            .ThenInclude(tt => tt.Event)
            .Include(t => t.OrderItem)
            .ThenInclude(oi => oi.Order)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Ticket> CreateAsync(Ticket entity, CancellationToken ct = default)
    {
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Ticket> UpdateAsync(Ticket entity, CancellationToken ct)
    {
        repository.Update(entity);
        await repository.SaveChangesAsync(ct);
        return entity;
    }
}
