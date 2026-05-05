using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using Radzen;
using TicketPlatform.Shared.Events;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public partial class CreateEventBase : ComponentBase
{
    protected CreateEventFormModel Model { get; set; } = new()
    {
        StartsAt = DateTime.Now
    };

    // Inject the HTTP client service to communicate with the API
    [Inject] protected IEventsClient EventsClient { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;


    protected async Task OnValidSubmit(EditContext editContext)
    {
        try
        {
            // changing timezones to avoid conflicts
            var startsAtUtc = Model.StartsAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(Model.StartsAt, DateTimeKind.Utc)
                : Model.StartsAt.ToUniversalTime();

            var createRequest = new CreateEventRequest(
                Title: Model.Title,
                Description: Model.Description,
                Location: Model.Location,
                StartsAt: startsAtUtc,
                Capacity: Model.Capacity
            );

            await EventsClient.CreateAsync(createRequest);

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Event Created",
                Detail = $"Event '{Model.Title}' has been successfully created!",
                Duration = 5000
            });

            // Reset the form
            Model = new CreateEventFormModel { StartsAt = DateTime.Now };

            NavigationManager.NavigateTo("/events");
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Failed to create event",
                Detail = ex.Message,
                Duration = 5000
            });
        }
    }

    protected Task OnInvalidSubmit(EditContext editContext)
    {
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Warning,
            Summary = "Validation Failed",
            Detail = "Please check all required fields and correct any errors.",
            Duration = 5000
        });
        return Task.CompletedTask;
    }
}

public class CreateEventFormModel
{
    [Required]
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1024, ErrorMessage = "Description cannot exceed 1024 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTime StartsAt { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; }
}
