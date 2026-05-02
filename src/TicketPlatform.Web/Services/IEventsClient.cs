using TicketPlatform.Shared.Events;

namespace TicketPlatform.Web.Services;

public interface IEventsClient
{
    Task<IReadOnlyList<EventDto>> GetAllAsync(CancellationToken ct = default);
    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EventDto?> CreateAsync(CreateEventRequest request, CancellationToken ct = default);
}
