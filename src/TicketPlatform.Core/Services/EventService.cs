using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public class EventService : IEventService
{
    private readonly IRepository<Event> _repository;

    public EventService(IRepository<Event> repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken ct = default)
        => _repository.ListAsync(ct);

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _repository.GetByIdAsync(id, ct);

    public async Task<Event> CreateAsync(Event @event, CancellationToken ct = default)
    {
        await _repository.AddAsync(@event, ct);
        await _repository.SaveChangesAsync(ct);
        return @event;
    }
}
