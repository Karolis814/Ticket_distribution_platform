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
        var result = await _http.GetFromJsonAsync<List<EventDto>>("api/events", ct);
        return result ?? new List<EventDto>();
    }

    public Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _http.GetFromJsonAsync<EventDto>($"api/events/{id}", ct);

    public async Task<EventDto?> CreateAsync(CreateEventRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/events", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventDto>(cancellationToken: ct);
    }
}
