using Microsoft.AspNetCore.Components;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public class HomeBase : ComponentBase
{
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] protected IEventsClient EventsClient { get; set; } = null!;

    protected IReadOnlyList<EventDto> PopularEvents { get; private set; } = [];
    protected IReadOnlyList<EventDto> LatestEvents { get; private set; } = [];
    protected bool IsLoading { get; private set; } = true;

    protected static DateTimeOffset StartDate(EventDto e) =>
        e.TicketTypes.Min(tt => tt.OccurenceStartDate);

    protected static DateTimeOffset EndDate(EventDto e) =>
        e.TicketTypes.Max(tt => tt.OccurenceEndDate);

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
            var result = await EventsClient.GetPagedAsync(page: 1, pageSize: 100);
            var all = result?.Items ?? [];

            PopularEvents = all
                .OrderByDescending(e => e.TicketTypes.Sum(tt => tt.Sold))
                .Take(5)
                .ToList();

            LatestEvents = all
                .OrderByDescending(e => e.CreatedAt)
                .Take(8)
                .ToList();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
