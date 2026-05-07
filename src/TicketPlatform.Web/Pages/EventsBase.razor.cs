using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using TicketPlatform.Shared.Events;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public class EventsBase : ComponentBase
{
    [Inject] protected IEventsClient eventsClient { get; set; } = default!;
    [Inject] protected NotificationService notificationService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected IReadOnlyList<EventDto> events { get; private set; } = Array.Empty<EventDto>();
    protected IReadOnlyList<EventDto> filteredEvents { get; private set; } = Array.Empty<EventDto>();
    protected IReadOnlyList<string> locations { get; private set; } = Array.Empty<string>();

    protected bool isLoading { get; private set; }

    protected string SearchText { get; set; } = string.Empty;
    protected string? SelectedLocation { get; set; }

    protected DateTime? FromDate { get; set; }
    protected DateTime? ToDate { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await loadEventsAsync();
    }

    private async Task loadEventsAsync()
    {
        isLoading = true;

        try
        {
            events = new List<EventDto>
            {
                new EventDto(Guid.NewGuid(), "Rock Concert", "Rock music event", "Vilnius", DateTime.Now.AddDays(5), 500),
                new EventDto(Guid.NewGuid(), "Basketball Finals", "Final basketball match", "Kaunas", DateTime.Now.AddDays(10), 1200),
                new EventDto(Guid.NewGuid(), "Tech Conference", "Technology and startups event", "Vilnius", DateTime.Now.AddDays(12), 300)
            };

            locations = events
                .Select(e => e.Location)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Distinct()
                .OrderBy(l => l)
                .ToList();

            filteredEvents = events;
        }
        catch (Exception ex)
        {
            notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Failed to load events",
                Detail = ex.Message,
                Duration = 5000
            });
        }
        finally
        {
            isLoading = false;
        }
    }

    protected void OnSearchInput(ChangeEventArgs e)
    {
        SearchText = e.Value?.ToString() ?? string.Empty;
    }

    protected void ApplyFilter()
    {
        IEnumerable<EventDto> query = events;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();

            query = query.Where(e =>
                Contains(e.Title, search) ||
                Contains(e.Description, search));
        }

        if (FromDate.HasValue)
        {
            query = query.Where(e => e.StartsAt.Date >= FromDate.Value.Date);
        }

        if (ToDate.HasValue)
        {
            query = query.Where(e => e.StartsAt.Date <= ToDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(SelectedLocation))
        {
            query = query.Where(e => e.Location == SelectedLocation);
        }

        filteredEvents = query.ToList();
    }


    private static bool Contains(string? value, string search)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}
