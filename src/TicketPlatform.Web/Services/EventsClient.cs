using System.Net.Http.Json;
using TicketPlatform.Shared;
using TicketPlatform.Shared.Dtos;

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
        var result = await _http.GetFromJsonAsync<PagedResult<EventDto>>(
            "api/events",
            ct);

        return result?.Items ?? Array.Empty<EventDto>();
    }

    public async Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<EventDto>(
            $"api/events/{id}",
            ct);
    }

    public async Task<EventDto?> CreateAsync(
        CreateEventRequest request,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/events",
            request,
            ct);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<EventDto>(
            cancellationToken: ct);
    }

    public async Task<IReadOnlyList<EventDto>> SearchAsync(
        string? title,
        DateTimeOffset? fromDate,
        string? location,
        string? category,
        CancellationToken ct = default)
    {
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(title))
            query.Add($"title={Uri.EscapeDataString(title)}");

        if (fromDate.HasValue)
            query.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.UtcDateTime.ToString("O"))}");

        if (!string.IsNullOrWhiteSpace(location))
            query.Add($"location={Uri.EscapeDataString(location)}");

        if (!string.IsNullOrWhiteSpace(category))
            query.Add($"category={Uri.EscapeDataString(category)}");

        var url = query.Count == 0
            ? "api/events"
            : $"api/events?{string.Join("&", query)}";

        var result = await _http.GetFromJsonAsync<PagedResult<EventDto>>(
            url,
            ct);

        return result?.Items ?? Array.Empty<EventDto>();
    }

    public async Task<IReadOnlyList<string>> GetLocationSuggestionsAsync(
        string input,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
            return Array.Empty<string>();

        var url =
            $"api/events/locations?input={Uri.EscapeDataString(input)}&page=1&pageSize=10";

        var result = await _http.GetFromJsonAsync<PagedResult<string>>(
            url,
            ct);

        return result?.Items ?? Array.Empty<string>();
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<IReadOnlyList<string>>(
            "api/events/categories",
            ct);

        return result ?? Array.Empty<string>();
    }
}
