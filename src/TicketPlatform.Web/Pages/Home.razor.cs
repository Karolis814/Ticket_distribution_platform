using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public class HomeBase : ComponentBase
{
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] protected IEventsClient EventsClient { get; set; } = null!;

    protected IReadOnlyList<EventDto> TrendingEvents { get; private set; } = [];
    protected IReadOnlyList<EventDto> LatestEvents { get; private set; } = [];
    protected IReadOnlyList<EventDto> UpcomingEvents { get; private set; } = [];
    protected bool IsLoading { get; private set; } = true;

    protected static DateTimeOffset StartDate(EventDto e) =>
        e.TicketTypes.Min(tt => tt.OccurenceStartDate).ToLocalTime();

    protected static DateTimeOffset EndDate(EventDto e) =>
        e.TicketTypes.Max(tt => tt.OccurenceEndDate).ToLocalTime();

    protected static int RemainingTickets(EventDto ev)
    {
        var total = ev.TicketTypes.Sum(t => t.Quantity);
        var sold = ev.TicketTypes.Sum(t => t.Sold);
        return Math.Max(0, total - sold);
    }

    protected static string? GetMinPriceText(EventDto ev)
    {
        if (!ev.TicketTypes.Any()) return null;
        var min = ev.TicketTypes.OrderBy(t => t.PriceCents).First();
        return min.PriceCents == 0
            ? "Free"
            : (min.PriceCents / 100m).ToString(
                $"0.00 {min.Currency.ToUpper()}",
                System.Globalization.CultureInfo.InvariantCulture);
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var popularTask = EventsClient.GetPopularAsync(count: 5);
            var latestTask = EventsClient.GetLatestAsync(count: 4);
            var upcomingTask = EventsClient.GetPagedAsync(page: 1, pageSize: 4);
            await Task.WhenAll(popularTask, latestTask, upcomingTask);
            TrendingEvents = await popularTask;
            LatestEvents = await latestTask;
            UpcomingEvents = (await upcomingTask)?.Items ?? [];
        }
        finally
        {
            IsLoading = false;
        }
    }
}
