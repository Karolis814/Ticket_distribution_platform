namespace TicketPlatform.Core.Events;

public interface IEventService
{
    Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken ct = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Event> CreateAsync(Event @event, CancellationToken ct = default);
}
