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

    [Inject] protected IEventsClient EventsClient { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected EventDto? Event { get; private set; }
    protected bool isLoading { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadEventAsync();
    }

    private async Task LoadEventAsync()
    {
        isLoading = true;
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
            isLoading = false;
        }
    }

    protected string FormatPrice(int priceCents, string currency)
    {
        var priceInDollars = priceCents / 100.0;
        return $"{currency} {priceInDollars:F2}";
    }

    protected Variant GetStatusVariant(EventStatus status)
    {
        return status switch
        {
            EventStatus.Draft => Variant.Outlined,
            EventStatus.Published => Variant.Flat,
            EventStatus.Cancelled => Variant.Filled,
            _ => Variant.Outlined
        };
    }
}
