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

    public async Task<TicketType> CreateAsync(TicketType ticketType, CancellationToken ct = default)
    {
        await repository.AddAsync(ticketType, ct);
        await repository.SaveChangesAsync(ct);
        return ticketType;
    }
}
