using Microsoft.AspNetCore.Components;
using Radzen;
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

    protected static DateTimeOffset StartDate(EventDto e) =>
        e.TicketTypes.Min(tt => tt.OccurenceStartDate).ToLocalTime();

    protected static int TotalSold(EventDto e) =>
        e.TicketTypes.Sum(tt => tt.Sold);

    protected static int TotalCapacity(EventDto e) =>
        e.TicketTypes.Sum(tt => tt.Quantity);

    protected static BadgeStyle StatusBadge(EventStatus status) => status switch
    {
        EventStatus.Published  => BadgeStyle.Success,
        EventStatus.Cancelled  => BadgeStyle.Danger,
        _                      => BadgeStyle.Light
    };
}
