using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface IEventService
{
    Task<(IReadOnlyList<Event> Items, int TotalCount)> GetUpcomingPagedAsync(int page, int pageSize,
        DateTimeOffset fromDate, CancellationToken ct = default);

    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Event> CreateAsync(Event @event, CancellationToken ct = default);
    Task<Event> UpdateAsync(Event @event, CancellationToken ct = default);
}
