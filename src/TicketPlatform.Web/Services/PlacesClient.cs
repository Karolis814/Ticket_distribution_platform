using System.Net.Http.Json;

namespace TicketPlatform.Web.Services;

public class PlacesClient : IPlacesClient
{
    private readonly HttpClient _http;

    public PlacesClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<PlacePredictionDto>> SearchAsync(
        string input,
        string? sessionToken = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<PlacePredictionDto>();

        try
        {
            var url = $"api/places/search?input={Uri.EscapeDataString(input)}";
            if (!string.IsNullOrEmpty(sessionToken))
                url += $"&sessionToken={Uri.EscapeDataString(sessionToken)}";

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = await _http.GetFromJsonAsync<List<PlacePredictionDto>>(url, options, ct);
            return result ?? new List<PlacePredictionDto>();
        }
        catch
        {
            return new List<PlacePredictionDto>();
        }
    }
}


