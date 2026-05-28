using Microsoft.AspNetCore.Components;
using Radzen;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Web.Components;

public partial class EventCardsBase : ComponentBase
{
    [Parameter] public IReadOnlyList<EventDto> Events { get; set; } = [];

    [Parameter] public int NumColsSm { get; set; } = 1;
    [Parameter] public int NumColsMd { get; set; } = 2;
    [Parameter] public int NumColsLg { get; set; } = 4;

    protected int ColSizeSm => 12 / NumColsSm;
    protected int ColSizeMd => 12 / NumColsMd;
    protected int ColSizeLg => 12 / NumColsLg;

    [Parameter] public bool IsLoading { get; set; } = true;
    [Parameter] public bool ShowTicketButton { get; set; } = true;
    [Parameter] public bool IsManagementView { get; set; } = false;
    [Parameter] public string EmptyMessage { get; set; } = "No events are currently available. Check back soon!";

    [Inject] private NavigationManager Nav { get; set; } = null!;

    protected void NavigateToDetails(Guid eventId) => Nav.NavigateTo($"/events/{eventId}");
    protected void NavigateToEdit(Guid eventId) => Nav.NavigateTo($"/host/events/{eventId}/edit");

    protected void OnCardClick(EventDto ev)
    {
        if (!IsManagementView)
            NavigateToDetails(ev.Id);
    }

    protected static int RemainingTickets(EventDto ev) =>
        Math.Max(0, ev.TicketTypes.Sum(t => t.Quantity) - ev.TicketTypes.Sum(t => t.Sold));

    protected static int TotalSold(EventDto ev) => ev.TicketTypes.Sum(t => t.Sold);
    protected static int TotalCapacity(EventDto ev) => ev.TicketTypes.Sum(t => t.Quantity);

    protected static DateTimeOffset StartDate(EventDto e) =>
        e.TicketTypes.Min(tt => tt.OccurenceStartDate).ToLocalTime();

    protected static DateTimeOffset EndDate(EventDto e) =>
        e.TicketTypes.Max(tt => tt.OccurenceEndDate).ToLocalTime();

    protected static bool IsEnded(EventDto ev) =>
        ev.Status == EventStatus.Published &&
        ev.TicketTypes.Any() &&
        EndDate(ev) < DateTimeOffset.Now;

    protected static string? GetMinPriceText(EventDto ev)
    {
        if (!ev.TicketTypes.Any()) return null;
        var min = ev.TicketTypes.OrderBy(t => t.PriceCents).First();
        return FormatPrice(min.PriceCents, min.Currency);
    }

    private static string FormatPrice(int cents, string currency = "USD") =>
        cents == 0
            ? "Free"
            : (cents / 100m).ToString($"0.00 {currency.ToUpper()}", System.Globalization.CultureInfo.InvariantCulture);

    protected static BadgeStyle StatusBadge(EventStatus status) => status switch
    {
        EventStatus.Published => BadgeStyle.Success,
        EventStatus.Cancelled => BadgeStyle.Danger,
        _                     => BadgeStyle.Light
    };
}
