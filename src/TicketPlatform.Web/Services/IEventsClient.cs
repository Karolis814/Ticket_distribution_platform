using TicketPlatform.Shared.Dtos;
using System.Text.Json.Serialization;

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
        string? category,
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
