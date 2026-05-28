using Microsoft.Extensions.Logging;
using TicketPlatform.Core.Services;

namespace TicketPlatform.Infrastructure.Services;

public class MockPlacesService(ILogger<MockPlacesService> logger) : IPlacesService
{
    private static readonly PlacePrediction[] CannedPredictions =
    [
        new("mock-vilnius-1", "Vilnius Town Hall", "Vilnius, Lithuania", "Didžioji g. 31, Vilnius"),
        new("mock-vilnius-2", "Cathedral Square", "Vilnius, Lithuania", "Katedros a., Vilnius"),
        new("mock-vilnius-3", "Gediminas Tower", "Vilnius, Lithuania", "Arsenalo g. 5, Vilnius"),
        new("mock-kaunas-1", "Kaunas Castle", "Kaunas, Lithuania", "Pilies g. 17, Kaunas"),
        new("mock-klaipeda-1", "Theatre Square", "Klaipėda, Lithuania", "Teatro a. 2, Klaipėda")
    ];

    public Task<IReadOnlyList<PlacePrediction>> SearchPlacesAsync(
        string input,
        string? sessionToken = null,
        CancellationToken ct = default)
    {
        logger.LogInformation("MockPlacesService.SearchPlacesAsync called with input={Input}", input);

        if (string.IsNullOrWhiteSpace(input))
            return Task.FromResult<IReadOnlyList<PlacePrediction>>(Array.Empty<PlacePrediction>());

        var matches = CannedPredictions
            .Where(p =>
                p.MainText.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                (p.Description?.Contains(input, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        return Task.FromResult<IReadOnlyList<PlacePrediction>>(matches);
    }

    public Task<PlaceDetails?> GetPlaceDetailsAsync(
        string placeId,
        string? sessionToken = null,
        CancellationToken ct = default)
    {
        logger.LogInformation("MockPlacesService.GetPlaceDetailsAsync called with placeId={PlaceId}", placeId);

        var prediction = CannedPredictions.FirstOrDefault(p => p.PlaceId == placeId);
        if (prediction is null)
            return Task.FromResult<PlaceDetails?>(null);

        return Task.FromResult<PlaceDetails?>(new PlaceDetails(
            prediction.PlaceId,
            prediction.MainText,
            StreetAddress: prediction.Description,
            PostalCode: "00000",
            City: prediction.SecondaryText?.Split(',').FirstOrDefault()?.Trim(),
            Country: "Lithuania",
            FormattedAddress: prediction.Description));
    }
}
