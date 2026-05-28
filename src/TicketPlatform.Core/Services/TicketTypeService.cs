using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public class TicketTypeService(IRepository<TicketType> repository) : ITicketTypeService
{
    public async Task<TicketType?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await repository.Query()
            .Include(tt => tt.Event)
            .Include(tt => tt.Tickets)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<bool> TryReserveAsync(Guid id, int quantity, CancellationToken ct = default)
    {
        var affected = await repository.Query()
            .Where(t =>
                t.Id == id &&
                t.Sold + quantity <= t.Quantity)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    t => t.Sold,
                    t => t.Sold + quantity),
                ct);

        return affected > 0;
    }
    public async Task<int> GetSoldTickets(Guid id, CancellationToken ct = default)
    {
        return await repository.Query()
            .Where(x => x.Id == id)
            .Select(x => x.Sold)
            .FirstOrDefaultAsync(ct);
    }
    public async Task<TicketType> CreateAsync(TicketType ticketType, CancellationToken ct = default)
    {
        await repository.AddAsync(ticketType, ct);
        await repository.SaveChangesAsync(ct);
        return ticketType;
    }
}
