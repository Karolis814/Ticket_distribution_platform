using System.Net.Http.Json;
using Radzen;
using TicketPlatform.Shared;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public class EventsClient(HttpClient http, NotificationService notify) : IEventsClient
{
    public async Task<PagedResult<EventDto>?> GetPagedAsync(
        int page,
        int pageSize,
        string? title = null,
        DateTimeOffset? fromDate = null,
        string? location = null,
        string? category = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(title))
                query.Add($"title={Uri.EscapeDataString(title)}");

            if (fromDate.HasValue)
                query.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.UtcDateTime.ToString("O"))}");

            if (!string.IsNullOrWhiteSpace(location))
                query.Add($"location={Uri.EscapeDataString(location)}");

            if (!string.IsNullOrWhiteSpace(category))
                query.Add($"category={Uri.EscapeDataString(category)}");

            return await http.GetFromJsonAsync<PagedResult<EventDto>>(
                $"api/events?{string.Join("&", query)}",
                ct);
        }
        catch (Exception ex)
        {
            Notify(ex, "Failed to load events");
            return null;
        }
    }

    public async Task<IReadOnlyList<EventDto>> GetAllAsync(
        CancellationToken ct = default)
    {
        try
        {
            var result = await http.GetFromJsonAsync<PagedResult<EventDto>>(
                "api/events",
                ct);

            return result?.Items ?? [];
        }
        catch (Exception ex)
        {
            Notify(ex, "Failed to load events");
            return [];
        }
    }

    public async Task<IReadOnlyList<EventDto>> GetMyEventsAsync(
        CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<IReadOnlyList<EventDto>>(
                "api/events/mine", ct) ?? [];
        }
        catch (Exception ex)
        {
            Notify(ex, "Failed to load your events");
            return [];
        }
    }

    public async Task<EventDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<EventDto>(
                $"api/events/{id}",
                ct);
        }
        catch (Exception ex)
        {
            Notify(ex, "Failed to load event");
            return null;
        }
    }

    public async Task<EventDto?> CreateAsync(
        CreateEventRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/events", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventDto>(cancellationToken: ct);
    }

    public async Task<EventDto?> UpdateAsync(
        Guid id,
        UpdateEventRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/events/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventDto>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<EventDto>> SearchAsync(
        string? title,
        DateTimeOffset? fromDate,
        string? location,
        string? category,
        CancellationToken ct = default)
    {
        try
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

            var result = await http.GetFromJsonAsync<PagedResult<EventDto>>(
                url,
                ct);

            return result?.Items ?? [];
        }
        catch (Exception ex)
        {
            Notify(ex, "Failed to search events");
            return [];
        }
    }

    public async Task<IReadOnlyList<string>> GetLocationSuggestionsAsync(
        string input,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
            return [];

        try
        {
            var url =
                $"api/events/locations?input={Uri.EscapeDataString(input)}&page=1&pageSize=10";

            var result = await http.GetFromJsonAsync<PagedResult<string>>(
                url,
                ct);

            return result?.Items ?? [];
        }
        catch (Exception ex)
        {
            Notify(ex, "Failed to load location suggestions");
            return [];
        }
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken ct = default)
    {
        try
        {
            var result = await http.GetFromJsonAsync<IReadOnlyList<string>>(
                "api/events/categories",
                ct);

            return result ?? [];
        }
        catch (Exception ex)
        {
            Notify(ex, "Failed to load categories");
            return [];
        }
    }

    private void Notify(Exception ex, string summary) =>
        notify.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Error,
            Summary = summary,
            Detail = ex.Message,
            Duration = 5000
        });
}
