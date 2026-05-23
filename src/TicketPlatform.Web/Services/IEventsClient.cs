using TicketPlatform.Shared;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public interface IEventsClient
{
    Task<PagedResult<EventDto>?> GetPagedAsync(
        int page,
        int pageSize,
        string? title = null,
        DateTimeOffset? fromDate = null,
        string? location = null,
        string? category = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<EventDto>> GetAllAsync(
        CancellationToken ct = default);

    Task<EventDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<EventDto?> CreateAsync(
        CreateEventRequest request,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetLocationSuggestionsAsync(
        string input,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken ct = default);
}
