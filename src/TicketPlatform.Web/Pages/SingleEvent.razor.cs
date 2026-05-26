using Microsoft.AspNetCore.Components;
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

    protected EventDto? Event { get; private set; }
    protected bool IsLoading { get; private set; }

    protected override async Task OnInitializedAsync()
    {
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

    private string FormatPrice(int priceCents, string currency)
    {
        var price = priceCents / 100m;
        return $"{price:0.00} {currency}";
    }

    protected string? GetStartingFromText(EventDto evt)
    {
        var ticketTypes = evt.TicketTypes;
        if (ticketTypes is null || ticketTypes.Count == 0)
        {
            return null;
        }

        var cheapest = ticketTypes.OrderBy(t => t.PriceCents).First();
        return $"Starting from {FormatPrice(cheapest.PriceCents, cheapest.Currency)}";
    }

    protected EventDisplayStatus GetDisplayStatus(EventDto evt)
    {
        if (evt.Status == EventStatus.Cancelled)
        {
            return EventDisplayStatus.Cancelled;
        }

        if (evt.TicketTypes is { Count: > 0 } &&
            evt.TicketTypes.All(t => t.Sold >= t.Quantity))
        {
            return EventDisplayStatus.SoldOut;
        }

        return EventDisplayStatus.EventAdded;
    }

    protected string GetStatusLabel(EventDisplayStatus status) => status switch
    {
        EventDisplayStatus.SoldOut => "Sold out",
        EventDisplayStatus.Cancelled => "Cancelled",
        EventDisplayStatus.EventAdded => "Event added",
        _ => status.ToString()
    };

    protected BadgeStyle GetStatusBadgeStyle(EventDisplayStatus status) => status switch
    {
        EventDisplayStatus.SoldOut => BadgeStyle.Warning,
        EventDisplayStatus.Cancelled => BadgeStyle.Danger,
        EventDisplayStatus.EventAdded => BadgeStyle.Success,
        _ => BadgeStyle.Light
    };

    protected bool ShouldShowCheckout(EventDto evt)
    {
        var status = GetDisplayStatus(evt);
        return status == EventDisplayStatus.EventAdded;
    }

    protected string GetHostDisplayName(HostDto host)
    {
        if (!string.IsNullOrWhiteSpace(host.Company))
            return host.Company!;
        var fullName = $"{host.FirstName} {host.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;
        return host.Email;
    }

    protected enum EventDisplayStatus
    {
        EventAdded,
        SoldOut,
        Cancelled
    }
}
