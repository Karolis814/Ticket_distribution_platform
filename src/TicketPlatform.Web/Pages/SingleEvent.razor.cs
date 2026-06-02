using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public partial class SingleEventBase : ComponentBase
{
    [Parameter]
    public Guid EventId { get; set; }

    [Inject] protected IEventsClient EventsClient { get; set; } = null!;
    [Inject] protected NotificationService NotificationService { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    [Inject] protected AuthenticationStateProvider AuthState { get; set; } = null!;

    protected EventDto? Event { get; private set; }
    protected bool IsLoading { get; private set; } = true;
    private Guid CurrentUserId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState.GetAuthenticationStateAsync();
        var sub = state.User.FindFirst("sub")?.Value;
        Guid.TryParse(sub, out var id);
        CurrentUserId = id;

        await LoadEventAsync();
    }

    private async Task LoadEventAsync()
    {
        IsLoading = true;
        try
        {
            Event = await EventsClient.GetByIdAsync(EventId);
            if (Event is null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Event not found",
                    Detail = "The requested event could not be found.",
                    Duration = 5000
                });
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Failed to load event",
                Detail = ex.Message,
                Duration = 5000
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected static string FormatPrice(int priceCents, string currency)
    {
        if (priceCents == 0) return "Free";
        var price = priceCents / 100m;
        return $"{price:0.00} {currency.ToUpper()}";
    }

    protected static string? GetMinPriceText(EventDto evt)
    {
        if (evt.TicketTypes is null || evt.TicketTypes.Count == 0) return null;
        var cheapest = evt.TicketTypes.OrderBy(t => t.PriceCents).First();
        return FormatPrice(cheapest.PriceCents, cheapest.Currency);
    }

    protected static string FormatDateRange(EventDto evt)
    {
        if (evt.TicketTypes is null || evt.TicketTypes.Count == 0)
            return "Date TBD";

        var start = evt.TicketTypes.Min(tt => tt.OccurenceStartDate).ToLocalTime();
        var end = evt.TicketTypes.Max(tt => tt.OccurenceEndDate).ToLocalTime();

        if (start.Date == end.Date)
            return $"{start:yyyy-MM-dd} · {start:HH:mm} – {end:HH:mm}";

        return $"{start:yyyy-MM-dd} – {end:yyyy-MM-dd}";
    }

    protected static int GetRemainingTickets(EventDto evt)
    {
        var total = evt.TicketTypes.Sum(t => t.Quantity);
        var sold = evt.TicketTypes.Sum(t => t.Sold);
        return Math.Max(0, total - sold);
    }

    protected static bool IsSoldOut(EventDto evt) =>
        evt.TicketTypes.Count > 0 && evt.TicketTypes.All(t => t.Sold >= t.Quantity);

    protected static bool IsEnded(EventDto evt) =>
        evt.TicketTypes.Count > 0 && evt.TicketTypes.Max(t => t.OccurenceEndDate) < DateTimeOffset.UtcNow;

    protected bool IsOwner(EventDto evt) =>
        CurrentUserId != Guid.Empty && CurrentUserId == evt.HostId;

    protected bool ShouldShowCheckout(EventDto evt) =>
        !IsOwner(evt) && evt.Status != EventStatus.Cancelled && !IsSoldOut(evt) && !IsEnded(evt);

    protected static string GetHostDisplayName(HostDto host)
    {
        if (!string.IsNullOrWhiteSpace(host.Company)) return host.Company!;
        var fullName = $"{host.FirstName} {host.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? host.Email : fullName;
    }

    protected static int GetTicketRemaining(TicketTypeDto tt) =>
        Math.Max(0, tt.Quantity - tt.Sold);
}
