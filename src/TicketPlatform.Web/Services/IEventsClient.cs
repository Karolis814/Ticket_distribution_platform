using TicketPlatform.Shared;
using TicketPlatform.Shared.Dtos;
using System.Text.Json.Serialization;

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

    Task<IReadOnlyList<EventDto>> GetPopularAsync(int count = 5, CancellationToken ct = default);
    Task<IReadOnlyList<EventDto>> GetLatestAsync(int count = 8, CancellationToken ct = default);

    Task<IReadOnlyList<EventDto>> GetAllAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<EventDto>> GetMyEventsAsync(
        CancellationToken ct = default);

    Task<EventDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<EventDto?> CreateAsync(
        CreateEventRequest request,
        CancellationToken ct = default);

    Task<EventDto?> UpdateAsync(
        Guid id,
        UpdateEventRequest request,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetLocationSuggestionsAsync(
        string input,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken ct = default);
}

public interface IPlacesClient
{
    Task<IReadOnlyList<PlacePredictionDto>> SearchAsync(string input, string? sessionToken = null, CancellationToken ct = default);
    Task<PlaceDetailsDto?> GetDetailsAsync(string placeId, string? sessionToken = null, CancellationToken ct = default);
}

public record PlacePredictionDto(
    [property: JsonPropertyName("placeId")] string PlaceId,
    [property: JsonPropertyName("mainText")] string MainText,
    [property: JsonPropertyName("secondaryText")] string SecondaryText,
    [property: JsonPropertyName("description")] string? Description
);

public record PlaceDetailsDto(
    [property: JsonPropertyName("placeId")] string PlaceId,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("streetAddress")] string? StreetAddress,
    [property: JsonPropertyName("postalCode")] string? PostalCode,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("formattedAddress")] string? FormattedAddress
);
