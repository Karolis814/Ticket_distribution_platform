using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketPlatform.Core.Services;

namespace TicketPlatform.Infrastructure.Services;

public class GooglePlacesService : IPlacesService
{
    private readonly HttpClient _httpClient;
    private readonly GooglePlacesOptions _options;
    private readonly ILogger<GooglePlacesService> _logger;

    public GooglePlacesService(HttpClient httpClient, IOptions<GooglePlacesOptions> options, ILogger<GooglePlacesService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PlacePrediction>> SearchPlacesAsync(string input, string? sessionToken = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Google Places API key is not configured.");
            return Array.Empty<PlacePrediction>();
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<PlacePrediction>();
        }

        try
        {
            var url = "https://maps.googleapis.com/maps/api/place/autocomplete/json";
            var query = new Dictionary<string, string>
            {
                { "input", input },
                { "key", _options.ApiKey }
            };

            if (!string.IsNullOrEmpty(sessionToken))
            {
                query["sessiontoken"] = sessionToken;
            }

            var queryString = string.Join("&", query.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            var response = await _httpClient.GetAsync($"{url}?{queryString}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Places API returned status {StatusCode}", response.StatusCode);
                return Array.Empty<PlacePrediction>();
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var result = System.Text.Json.JsonSerializer.Deserialize<GooglePlacesAutocompleteResponse>(content, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower });

            if (result?.Predictions == null)
            {
                return Array.Empty<PlacePrediction>();
            }

            return result.Predictions.Select(p => new PlacePrediction(
                p.PlaceId,
                p.StructuredFormatting?.MainText ?? string.Empty,
                p.StructuredFormatting?.SecondaryText ?? string.Empty,
                p.Description
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching places with input '{Input}'", input);
            return Array.Empty<PlacePrediction>();
        }
    }
}

internal record GooglePlacesAutocompleteResponse(
    [property: JsonPropertyName("predictions")] IReadOnlyList<GooglePlacesPrediction> Predictions
);

internal record GooglePlacesPrediction(
    [property: JsonPropertyName("place_id")] string PlaceId,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("structured_formatting")] StructuredFormatting StructuredFormatting
);

internal record StructuredFormatting(
    [property: JsonPropertyName("main_text")] string MainText,
    [property: JsonPropertyName("secondary_text")] string? SecondaryText
);

