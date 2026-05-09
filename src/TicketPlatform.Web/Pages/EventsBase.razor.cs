using Microsoft.AspNetCore.Components;
using Radzen;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public class EventsBase : ComponentBase
{
    [Inject] protected IEventsClient eventsClient { get; set; } = default!;
    [Inject] protected NotificationService notificationService { get; set; } = default!;

    protected IReadOnlyList<EventDto> events { get; private set; } = Array.Empty<EventDto>();
    protected bool isLoading { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        await loadEventsAsync();
    }

    private async Task loadEventsAsync()
    {
        isLoading = true;
        try
        {
            events = await eventsClient.GetAllAsync();
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
}
