using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public interface IEventsClient
{
    Task<IReadOnlyList<EventDto>> GetAllAsync(
        CancellationToken ct = default);

    Task<EventDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<EventDto?> CreateAsync(
        CreateEventRequest request,
        CancellationToken ct = default);

    Task<IReadOnlyList<EventDto>> SearchAsync(
        string? title,
        DateTimeOffset? fromDate,
        string? location,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetLocationSuggestionsAsync(
        string input,
        CancellationToken ct = default);
}
