using Microsoft.AspNetCore.Components;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Components;

public partial class EventCardsBase : ComponentBase
{
    [Parameter] public IReadOnlyList<EventDto> Events { get; set; } = [];

    [Parameter] public int Size { get; set; } = 4;
    [Parameter] public int NumColsSm { get; set; } = 1;
    [Parameter] public int NumColsMd { get; set; } = 2;
    [Parameter] public int NumColsLg { get; set; } = 4;
    protected int ColSizeSm => 12 / NumColsSm;
    protected int ColSizeMd => 12 / NumColsMd;
    protected int ColSizeLg => 12 / NumColsLg;

    [Parameter] public bool IsLoading { get; set; } = true;

    [Inject] private NavigationManager Nav { get; set; } = null!;


    protected void NavigateToCheckout(Guid eventId)
    {
        Nav.NavigateTo($"/checkout/{eventId}");
    }

    protected static string GetDescriptionExcerpt(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        return description.Length > 100
            ? $"{description[..97]}..."
            : description;
    }

    protected static int RemainingTickets(EventDto ev)
    {
        var total = ev.TicketTypes.Sum(t => t.Quantity);
        var sold = ev.TicketTypes.Sum(t => t.Sold);

        return Math.Max(0, total - sold);
    }

    protected static DateTimeOffset StartDate(EventDto e) =>
        e.TicketTypes.Min(tt => tt.OccurenceStartDate);

    protected static DateTimeOffset EndDate(EventDto e) =>
        e.TicketTypes.Max(tt => tt.OccurenceEndDate);

    protected static string GetStartingPriceText(EventDto ev)
    {
        if (ev.TicketTypes.Count == 0)
            return "Get Tickets";

        var minPriceTicket =
            ev.TicketTypes.OrderBy(t => t.PriceCents).First();

        return
            $"Starting from {FormatPrice(minPriceTicket.PriceCents, minPriceTicket.Currency)}";
    }

    private static string FormatPrice(
        int cents,
        string currency = "USD") =>
        cents == 0
            ? "Free"
            : (cents / 100m).ToString(
                $"0.00 {currency.ToUpper()}",
                System.Globalization.CultureInfo.InvariantCulture);
}
