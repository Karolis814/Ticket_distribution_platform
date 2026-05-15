using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public class OrderService(IRepository<Order> repository) : IOrderService
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await repository.Query()
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Tickets)
            .ThenInclude(t => t.TicketType)
            .ThenInclude(tt => tt.Event)
            .ThenInclude(e => e.Host)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Order> CreateAsync(Order entity, CancellationToken ct = default)
    {
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Order> UpdateAsync(Order entity, CancellationToken ct = default)
    {
        repository.Update(entity);
        await repository.SaveChangesAsync(ct);
        return entity;
    }
}
