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
                BuildSecondaryText(p),
                p.Description
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching places with input '{Input}'", input);
            return Array.Empty<PlacePrediction>();
        }
    }

    public async Task<PlaceDetails?> GetPlaceDetailsAsync(string placeId, string? sessionToken = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Google Places API key is not configured.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(placeId))
        {
            return null;
        }

        try
        {
            var url = "https://maps.googleapis.com/maps/api/place/details/json";
            var query = new Dictionary<string, string>
            {
                { "place_id", placeId },
                { "key", _options.ApiKey },
                { "fields", "place_id,name,formatted_address,address_component" }
            };

            if (!string.IsNullOrEmpty(sessionToken))
            {
                query["sessiontoken"] = sessionToken;
            }

            var queryString = string.Join("&", query.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            var response = await _httpClient.GetAsync($"{url}?{queryString}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Places Details API returned status {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var result = System.Text.Json.JsonSerializer.Deserialize<GooglePlaceDetailsResponse>(content, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower });

            if (result?.Result == null)
            {
                return null;
            }

            var components = result.Result.AddressComponents ?? new List<GoogleAddressComponent>();

            string? streetNumber = FindComponent(components, "street_number");
            string? route = FindComponent(components, "route");
            string? postalCode = FindComponent(components, "postal_code");
            string? city = FindComponent(components, "locality")
                           ?? FindComponent(components, "postal_town")
                           ?? FindComponent(components, "administrative_area_level_2");
            string? country = FindComponent(components, "country");

            string? streetAddress = (streetNumber, route) switch
            {
                ({ Length: > 0 }, { Length: > 0 }) => $"{route} {streetNumber}",
                (_, { Length: > 0 }) => route,
                ({ Length: > 0 }, _) => streetNumber,
                _ => null
            };

            return new PlaceDetails(
                result.Result.PlaceId ?? placeId,
                result.Result.Name,
                streetAddress,
                postalCode,
                city,
                country,
                result.Result.FormattedAddress
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching place details for placeId '{PlaceId}'", placeId);
            return null;
        }
    }

    private static string BuildSecondaryText(GooglePlacesPrediction prediction)
    {
        var secondary = prediction.StructuredFormatting?.SecondaryText;
        var description = prediction.Description;

        if (!string.IsNullOrEmpty(description) && HasDigit(description))
        {
            var main = prediction.StructuredFormatting?.MainText;
            if (!string.IsNullOrEmpty(main) && description.StartsWith(main + ", ", StringComparison.OrdinalIgnoreCase))
            {
                return description.Substring(main.Length + 2);
            }
            return description;
        }

        return secondary ?? string.Empty;
    }

    private static bool HasDigit(string value)
    {
        foreach (var c in value)
        {
            if (char.IsDigit(c)) return true;
        }
        return false;
    }

    private static string? FindComponent(IEnumerable<GoogleAddressComponent> components, string type)
    {
        var match = components.FirstOrDefault(c => c.Types != null && c.Types.Contains(type));
        return match?.LongName;
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

internal record GooglePlaceDetailsResponse(
    [property: JsonPropertyName("result")] GooglePlaceDetailsResult? Result
);

internal record GooglePlaceDetailsResult(
    [property: JsonPropertyName("place_id")] string? PlaceId,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("formatted_address")] string? FormattedAddress,
    [property: JsonPropertyName("address_components")] List<GoogleAddressComponent>? AddressComponents
);

internal record GoogleAddressComponent(
    [property: JsonPropertyName("long_name")] string? LongName,
    [property: JsonPropertyName("short_name")] string? ShortName,
    [property: JsonPropertyName("types")] List<string>? Types
);
