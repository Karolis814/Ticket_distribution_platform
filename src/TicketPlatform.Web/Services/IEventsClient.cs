using TicketPlatform.Shared.Dtos;
using System.Text.Json.Serialization;

namespace TicketPlatform.Web.Services;

public interface IEventsClient
{
    Task<IReadOnlyList<EventDto>> GetAllAsync(CancellationToken ct = default);
    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EventDto?> CreateAsync(CreateEventRequest request, CancellationToken ct = default);
}

public interface IPlacesClient
{
    Task<IReadOnlyList<PlacePredictionDto>> SearchAsync(string input, string? sessionToken = null, CancellationToken ct = default);
}

public record PlacePredictionDto(
    [property: JsonPropertyName("placeId")] string PlaceId,
    [property: JsonPropertyName("mainText")] string MainText,
    [property: JsonPropertyName("secondaryText")] string SecondaryText,
    [property: JsonPropertyName("description")] string? Description
);


