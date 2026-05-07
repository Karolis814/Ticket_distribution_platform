using Microsoft.AspNetCore.Components;
using Radzen;
using TicketPlatform.Shared.Events;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public partial class CheckoutBase : ComponentBase
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

    protected void OnPurchase()
    {
        try
        {
            // Placeholder for purchase logic
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Ticket Purchased",
                Detail = $"You have successfully purchased a ticket for {Event?.Title}!",
                Duration = 5000
            });

            // Redirect back to events page after successful purchase
            NavigationManager.NavigateTo("/events");
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Purchase Failed",
                Detail = ex.Message,
                Duration = 5000
            });
        }
    }
}

