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

    protected IReadOnlyList<EventDto> filteredEvents { get; private set; } = Array.Empty<EventDto>();
    protected IReadOnlyList<string> locationSuggestions { get; private set; } = Array.Empty<string>();

    protected bool isLoading { get; private set; }

    protected string SearchText { get; set; } = string.Empty;
    protected string LocationText { get; set; } = string.Empty;

    protected DateTime? FromDate { get; set; }
    protected DateTime? ToDate { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await ApplyFilterAsync();
    }

    protected void OnSearchInput(ChangeEventArgs e)
    {
        SearchText = e.Value?.ToString() ?? string.Empty;
    }

    protected async Task LoadLocationSuggestions(LoadDataArgs args)
    {
        var input = args.Filter ?? string.Empty;

        LocationText = input;

        locationSuggestions = await eventsClient.GetLocationSuggestionsAsync(input);
    }

    protected async Task ApplyFilterAsync()
    {
        isLoading = true;

        try
        {
            filteredEvents = await eventsClient.SearchAsync(
                SearchText,
                FromDate,
                ToDate,
                LocationText);
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

    protected async Task ClearFilterAsync()
    {
        SearchText = string.Empty;
        LocationText = string.Empty;
        FromDate = null;
        ToDate = null;
        locationSuggestions = Array.Empty<string>();

        await ApplyFilterAsync();
    }
}
