using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Core.Services;

public class EventService(IRepository<Event> repository) : IEventService
{
    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> GetUpcomingPagedAsync(
        int page,
        int pageSize,
        DateTimeOffset fromDate,
        string? category,
        CancellationToken ct = default){

        var baseQuery = repository.Query()
            .Where(e =>
                e.Status == EventStatus.Published &&
                e.TicketTypes.Max(tt => tt.OccurenceEndDate) >= fromDate);

        if (!string.IsNullOrWhiteSpace(category))
        {
            baseQuery = baseQuery.Where(e => e.Category == category);
        }

        baseQuery = baseQuery
            .OrderBy(e => e.TicketTypes.Min(tt => tt.OccurenceStartDate))
            .AsNoTracking();

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .Include(e => e.TicketTypes)
            .ThenInclude(tt => tt.Tickets)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await repository.Query()
            .Include(e => e.Host)
            .Include(e => e.TicketTypes)
            .ThenInclude(tt => tt.Tickets)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Event> CreateAsync(Event @event, CancellationToken ct = default)
    {
        await repository.AddAsync(@event, ct);
        await repository.SaveChangesAsync(ct);
        return @event;
    }

    public async Task<Event> UpdateAsync(Event @event, CancellationToken ct = default)
    {
        repository.Update(@event);
        await repository.SaveChangesAsync(ct);
        return @event;
    }
}
