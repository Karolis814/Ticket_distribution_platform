using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;

namespace TicketPlatform.Infrastructure.Caching;

public class CachingEventServiceDecorator(
    IEventService inner,
    IMemoryCache cache,
    ILogger<CachingEventServiceDecorator> logger) : IEventService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> GetUpcomingPagedAsync(
        int page,
        int pageSize,
        DateTimeOffset fromDate,
        string? title,
        string? location,
        string? category,
        CancellationToken ct = default)
    {
        var key = $"events:upcoming:{page}:{pageSize}:{fromDate.UtcDateTime:yyyy-MM-ddTHH:mm}:{title}:{location}:{category}";

        if (cache.TryGetValue(key, out (IReadOnlyList<Event> Items, int TotalCount) cached))
        {
            logger.LogInformation("[Cache HIT] {Key}", key);
            return cached;
        }

        logger.LogInformation("[Cache MISS] {Key}", key);
        var result = await inner.GetUpcomingPagedAsync(page, pageSize, fromDate, title, location, category, ct);
        cache.Set(key, result, Ttl);
        return result;
    }

    public Task<(IReadOnlyList<string> Items, int TotalCount)> GetLocationSuggestionsAsync(
        string input,
        int page,
        int pageSize,
        CancellationToken ct = default)
        => inner.GetLocationSuggestionsAsync(input, page, pageSize, ct);

    public Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default)
        => inner.GetCategoriesAsync(ct);

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => inner.GetByIdAsync(id, ct);

    public Task<Event> CreateAsync(Event @event, CancellationToken ct = default)
        => inner.CreateAsync(@event, ct);

    public Task<Event> UpdateAsync(Event @event, CancellationToken ct = default)
        => inner.UpdateAsync(@event, ct);
}
