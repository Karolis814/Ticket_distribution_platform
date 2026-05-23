namespace TicketPlatform.Core.Services;

public interface IPlacesService
{
    Task<IReadOnlyList<PlacePrediction>> SearchPlacesAsync(string input, string? sessionToken = null, CancellationToken ct = default);
    Task<PlaceDetails?> GetPlaceDetailsAsync(string placeId, string? sessionToken = null, CancellationToken ct = default);
}

public record PlacePrediction(
    string PlaceId,
    string MainText,
    string SecondaryText,
    string? Description
);

public record PlaceDetails(
    string PlaceId,
    string? Name,
    string? StreetAddress,
    string? PostalCode,
    string? City,
    string? Country,
    string? FormattedAddress
);
