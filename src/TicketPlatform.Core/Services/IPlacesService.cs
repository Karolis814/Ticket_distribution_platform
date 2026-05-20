namespace TicketPlatform.Core.Services;

public interface IPlacesService
{
    Task<IReadOnlyList<PlacePrediction>> SearchPlacesAsync(string input, string? sessionToken = null, CancellationToken ct = default);
}

public record PlacePrediction(
    string PlaceId,
    string MainText,
    string SecondaryText,
    string? Description
);

