using Microsoft.AspNetCore.Components;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public class MyEventsBase : ComponentBase
{
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] protected IEventsClient EventsClient { get; set; } = null!;

    protected List<EventDto> Events = [];
    protected bool IsLoading = true;

    protected override async Task OnInitializedAsync()
    {
        Events = (await EventsClient.GetMyEventsAsync()).ToList();
        IsLoading = false;
    }

    protected static bool IsEnded(EventDto ev) =>
        ev.Status == EventStatus.Published &&
        ev.TicketTypes.Any() &&
        ev.TicketTypes.Max(tt => tt.OccurenceEndDate).ToLocalTime() < DateTimeOffset.Now;
}
