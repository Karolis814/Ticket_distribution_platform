using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Core.Services;

public class EventService(IRepository<Event> repository) : IEventService
{
    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> GetUpcomingPagedAsync(
        int page,
        int pageSize,
        DateTimeOffset fromDate,
        string? title,
        string? location,
        string? category,
        CancellationToken ct = default)
    {
        var query = repository.Query()
            .Where(e =>
                e.Status == EventStatus.Published &&
                e.TicketTypes.Max(tt => tt.OccurenceEndDate) >= fromDate);

        if (!string.IsNullOrWhiteSpace(title))
        {
            var titleFilter = title.ToLower();

            query = query.Where(e =>
                e.Title.ToLower().Contains(titleFilter) ||
                e.Description.ToLower().Contains(titleFilter));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var locationFilter = location.ToLower();

            query = query.Where(e =>
                e.Location != null &&
                e.Location.ToLower().Contains(locationFilter));
        }

        if (!string.IsNullOrWhiteSpace(category) &&
            Enum.TryParse<EventCategory>(category, true, out var parsedCategory))
        {
            query = query.Where(e => e.Category == parsedCategory);
        }

        query = query
            .OrderBy(e => e.TicketTypes.Min(tt => tt.OccurenceStartDate))
            .AsNoTracking();

        var total = await query.CountAsync(ct);

        var items = await query
            .Include(e => e.Host)
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Tickets)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(IReadOnlyList<string> Items, int TotalCount)> GetLocationSuggestionsAsync(
        string input,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
            return (Array.Empty<string>(), 0);

        var inputFilter = input.ToLower();

        var query = repository.Query()
            .Where(e =>
                e.Status == EventStatus.Published &&
                e.Location != null &&
                e.Location.ToLower().Contains(inputFilter))
            .Select(e => e.Location!)
            .Distinct()
            .OrderBy(l => l)
            .AsNoTracking();

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken ct = default)
    {
        IReadOnlyList<string> categories = Enum.GetNames<EventCategory>()
            .OrderBy(c => c)
            .ToList();

        return Task.FromResult(categories);
    }

    public async Task<IReadOnlyList<Event>> GetPopularAsync(int count, CancellationToken ct = default)
        => await repository.Query()
            .Where(e => e.Status == EventStatus.Published &&
                        e.TicketTypes.Max(tt => tt.OccurenceEndDate) >= DateTimeOffset.UtcNow)
            .OrderByDescending(e =>
                e.TicketTypes
                    .SelectMany(tt => tt.Tickets)
                    .Select(t => t.OrderItem.OrderId)
                    .Distinct()
                    .Count())
            .Take(count)
            .Include(e => e.Host)
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Tickets)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Event>> GetLatestAsync(int count, CancellationToken ct = default)
        => await repository.Query()
            .Where(e => e.Status == EventStatus.Published &&
                        e.TicketTypes.Max(tt => tt.OccurenceEndDate) >= DateTimeOffset.UtcNow)
            .OrderByDescending(e => e.CreatedAt)
            .Take(count)
            .Include(e => e.Host)
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Tickets)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Event>> GetByHostAsync(Guid hostId, CancellationToken ct = default)
        => await repository.Query()
            .Where(e => e.HostId == hostId)
            .Include(e => e.Host)
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Tickets)
            .OrderByDescending(e => e.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

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

    public async Task<Event?> UpdateAsync(Guid id, UpdateEventRequest request, CancellationToken ct = default)
    {
        var @event = await repository.Query()
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Tickets)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (@event is null) return null;

        @event.Category = request.Category;
        @event.Title = request.Title;
        @event.Description = request.Description;
        @event.Location = request.Location;
        @event.ThumbnailUrl = request.ThumbnailUrl;
        if (@event.Status != EventStatus.Cancelled)
            @event.Status = request.Status;

        var requestIds = request.TicketTypes
            .Where(t => t.Id.HasValue)
            .Select(t => t.Id!.Value)
            .ToHashSet();

        foreach (var tt in @event.TicketTypes
            .Where(tt => !requestIds.Contains(tt.Id) && tt.Tickets.Count == 0)
            .ToList())
        {
            @event.TicketTypes.Remove(tt);
        }

        foreach (var req in request.TicketTypes)
        {
            if (req.Id.HasValue)
            {
                var existing = @event.TicketTypes.FirstOrDefault(tt => tt.Id == req.Id.Value);
                if (existing is not null)
                {
                    existing.Title = req.Title;
                    existing.OccurenceStartDate = req.OccurenceStartDate.ToUniversalTime();
                    existing.OccurenceEndDate = req.OccurenceEndDate.ToUniversalTime();
                    existing.AdmissionStartDate = req.AdmissionStartDate.ToUniversalTime();
                    existing.AdmissionEndDate = req.AdmissionEndDate.ToUniversalTime();
                    existing.PriceCents = req.PriceCents;
                    existing.Currency = req.Currency;
                    existing.MaxUses = req.MaxUses;
                    existing.Quantity = req.Quantity;
                }
            }
            else
            {
                @event.TicketTypes.Add(new TicketType
                {
                    Title = req.Title,
                    OccurenceStartDate = req.OccurenceStartDate.ToUniversalTime(),
                    OccurenceEndDate = req.OccurenceEndDate.ToUniversalTime(),
                    AdmissionStartDate = req.AdmissionStartDate.ToUniversalTime(),
                    AdmissionEndDate = req.AdmissionEndDate.ToUniversalTime(),
                    PriceCents = req.PriceCents,
                    Currency = req.Currency,
                    MaxUses = req.MaxUses,
                    Quantity = req.Quantity
                });
            }
        }

        await repository.SaveChangesAsync(ct);
        return await repository.Query()
            .Include(e => e.Host)
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Tickets)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }
}
