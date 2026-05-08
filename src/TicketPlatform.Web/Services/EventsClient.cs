using System.Net.Http.Json;
using TicketPlatform.Shared.Events;

namespace TicketPlatform.Web.Services;

public class EventsClient : IEventsClient
{
    private readonly HttpClient _http;

    public EventsClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<EventDto>> GetAllAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<IReadOnlyList<EventDto>>("api/events", ct);
        return result ?? Array.Empty<EventDto>();
    }

    public async Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<EventDto>($"api/events/{id}", ct);
    }

    public async Task<EventDto?> CreateAsync(CreateEventRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/events", request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<EventDto>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<EventDto>> SearchAsync(
        string? title,
        DateTime? fromDate,
        DateTime? toDate,
        string? location,
        CancellationToken ct = default)
    {
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(title))
            query.Add($"title={Uri.EscapeDataString(title)}");

        if (fromDate.HasValue)
            query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");

        if (toDate.HasValue)
            query.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        if (!string.IsNullOrWhiteSpace(location))
            query.Add($"location={Uri.EscapeDataString(location)}");

        var url = query.Count == 0
            ? "api/events"
            : $"api/events?{string.Join("&", query)}";

        var result = await _http.GetFromJsonAsync<IReadOnlyList<EventDto>>(url, ct);
        return result ?? Array.Empty<EventDto>();
    }

    public async Task<IReadOnlyList<string>> GetLocationSuggestionsAsync(
        string input,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
        {
            return Array.Empty<string>();
        }

        var url = $"api/events/locations?input={Uri.EscapeDataString(input)}";

        var result = await _http.GetFromJsonAsync<IReadOnlyList<string>>(url, ct);
        return result ?? Array.Empty<string>();
    }
}
