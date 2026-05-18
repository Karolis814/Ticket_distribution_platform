using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using Radzen;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public partial class CreateEventBase : ComponentBase
{
    protected CreateEventFormModel Model { get; set; } = new()
    {
        Status = EventStatus.Draft,
        TicketReleases = new List<TicketReleaseModel> { new() }
    };

    protected List<string> Categories { get; set; } = new()
    {
        "Conference",
        "Concert",
        "Theater",
        "Sports",
        "Workshop",
        "Meetup",
        "Festival",
        "Exhibition",
        "Other"
    };

    protected List<EventStatus> EventStatuses { get; set; } = new()
    {
        EventStatus.Draft,
        EventStatus.Published,
        EventStatus.Cancelled
    };

    // Inject the HTTP client service to communicate with the API
    [Inject] protected IEventsClient EventsClient { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected HttpClient HttpClient { get; set; } = default!;

    protected Guid CurrentUserId { get; set; } = Guid.Empty;
    protected bool IsInitialized { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        /*
        try
        {
            // Try to get the first user from the API
            // In a real app, this would be authenticated user info
            var response = await HttpClient.GetAsync("api/users?pageSize=1");
            if (response.IsSuccessStatusCode)
            {
                // For now, we'll use a hardcoded approach
                // This is temporary - in production, use proper authentication
                CurrentUserId = Guid.Parse("284528b2-9266-4e13-978c-67238952e543");
            }
        }
        catch
        {
            // Use default user ID if API call fails
            CurrentUserId = Guid.Parse("284528b2-9266-4e13-978c-67238952e543");
        }
*/
        CurrentUserId = Guid.Parse("284528b2-9266-4e13-978c-67238952e543");
        IsInitialized = true;
    }

    protected async Task OnValidSubmit(EditContext editContext)
    {
        try
        {
            if (!Model.TicketReleases.Any())
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validation Failed",
                    Detail = "At least one ticket release is required.",
                    Duration = 5000
                });
                return;
            }

            // Validate ticket releases
            foreach (var release in Model.TicketReleases)
            {
                if (release.OccurenceEndDate <= release.OccurenceStartDate)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Validation Failed",
                        Detail = $"Release '{release.Title}': End date must be after start date.",
                        Duration = 5000
                    });
                    return;
                }

                if (release.AdmissionEndDate <= release.AdmissionStartDate)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Validation Failed",
                        Detail = $"Release '{release.Title}': Admission end date must be after admission start date.",
                        Duration = 5000
                    });
                    return;
                }

                if (release.Quantity < 1)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Validation Failed",
                        Detail = $"Release '{release.Title}': Quantity must be at least 1.",
                        Duration = 5000
                    });
                    return;
                }

                if (release.PriceCents < 0)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Validation Failed",
                        Detail = $"Release '{release.Title}': Price cannot be negative.",
                        Duration = 5000
                    });
                    return;
                }
            }

            var ticketTypes = Model.TicketReleases.Select(tt => new CreateTicketTypeRequest(
                EventId: Guid.Empty, // Will be set by the server
                Title: tt.Title,
                OccurenceStartDate: tt.OccurenceStartDate,
                OccurenceEndDate: tt.OccurenceEndDate,
                AdmissionStartDate: tt.AdmissionStartDate,
                AdmissionEndDate: tt.AdmissionEndDate,
                PriceCents: tt.PriceCents,
                Currency: tt.Currency,
                MaxUses: tt.MaxUses,
                Quantity: tt.Quantity
            )).ToList();

            var createRequest = new CreateEventRequest(
                HostId: CurrentUserId,
                Category: Model.Category,
                Title: Model.Title,
                Description: Model.Description,
                Location: Model.Location,
                ThumbnailUrl: Model.ThumbnailUrl,
                Status: Model.Status,
                TicketTypes: ticketTypes
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
            Model = new CreateEventFormModel
            {
                Status = EventStatus.Draft,
                TicketReleases = new List<TicketReleaseModel> { new() }
            };

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

    protected void AddTicketRelease()
    {
        Model.TicketReleases.Add(new TicketReleaseModel());
    }

    protected void RemoveTicketRelease(int index)
    {
        if (Model.TicketReleases.Count > 1)
        {
            Model.TicketReleases.RemoveAt(index);
        }
        else
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Cannot Remove",
                Detail = "At least one ticket release is required.",
                Duration = 5000
            });
        }
    }

    protected async Task OnFileChange(InputFileChangeEventArgs e)
    {
        try
        {
            var file = e.GetMultipleFiles(1).FirstOrDefault();
            if (file != null)
            {
                // Store the file name for now (not binding to anything as per requirements)
                Model.ThumbnailFileName = file.Name;
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Info,
                    Summary = "File Selected",
                    Detail = $"Image '{file.Name}' has been selected for the event.",
                    Duration = 3000
                });
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "File Upload Error",
                Detail = ex.Message,
                Duration = 5000
            });
        }
    }
}

public class CreateEventFormModel
{
    [Required]
    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters.")]
    public string Category { get; set; } = string.Empty;

    [Required]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(10000, ErrorMessage = "Description cannot exceed 10000 characters.")]
    public string Description { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Location cannot exceed 300 characters.")]
    public string? Location { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Draft;

    public string? ThumbnailUrl { get; set; }

    public string? ThumbnailFileName { get; set; } = string.Empty;

    public List<TicketReleaseModel> TicketReleases { get; set; } = new();
}

public class TicketReleaseModel
{
    [Required]
    [StringLength(100, ErrorMessage = "Ticket type title cannot exceed 100 characters.")]
    public string Title { get; set; } = "Standard Ticket";

    [Required]
    public DateTimeOffset OccurenceStartDate { get; set; } = DateTimeOffset.UtcNow.AddDays(1);

    [Required]
    public DateTimeOffset OccurenceEndDate { get; set; } = DateTimeOffset.UtcNow.AddDays(2);

    [Required]
    public DateTimeOffset AdmissionStartDate { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public DateTimeOffset AdmissionEndDate { get; set; } = DateTimeOffset.UtcNow.AddDays(1);

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Price cannot be negative.")]
    public int PriceCents { get; set; } = 0;

    [Required]
    [StringLength(3, ErrorMessage = "Currency must be 3 characters.")]
    public string Currency { get; set; } = "USD";

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Max uses must be at least 1.")]
    public int MaxUses { get; set; } = 1;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; } = 100;
}
