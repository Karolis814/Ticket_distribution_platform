using TicketPlatform.Core.Entities;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Core.Services;

public interface IEventService
{
    Task<(IReadOnlyList<Event> Items, int TotalCount)> GetUpcomingPagedAsync(
        int page,
        int pageSize,
        DateTimeOffset fromDate,
        string? title,
        string? location,
        string? category,
        CancellationToken ct = default);

    Task<(IReadOnlyList<string> Items, int TotalCount)> GetLocationSuggestionsAsync(
        string input,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<Event>> GetByHostAsync(Guid hostId, CancellationToken ct = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Event> CreateAsync(Event @event, CancellationToken ct = default);
    Task<Event> UpdateAsync(Event @event, CancellationToken ct = default);
    Task<Event?> UpdateAsync(Guid id, UpdateEventRequest request, CancellationToken ct = default);
}
