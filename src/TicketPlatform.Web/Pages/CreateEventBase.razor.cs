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

    protected List<string> Currencies { get; set; } = new()
    {
        "EUR",
        "USD",
        "GBP"
    };

    [Inject] protected IEventsClient EventsClient { get; set; } = default!;
    [Inject] protected IPlacesClient PlacesClient { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected HttpClient HttpClient { get; set; } = default!;

    protected Guid CurrentUserId { get; set; } = Guid.Empty;
    protected bool IsInitialized { get; set; } = false;
    protected List<PlacePredictionDto> LocationSuggestions { get; set; } = new();
    protected bool IsSearchingLocations { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        await GetCurrentUserIdAsync();
        IsInitialized = true;
    }

    private async Task GetCurrentUserIdAsync()
    {
        try
        {
            // Try to get the current user from the API
            // This will work once authentication is properly set up
            var response = await HttpClient.GetAsync("api/users/me");
            if (response.IsSuccessStatusCode)
            {
                // Parse the actual user ID from the response
                var content = await response.Content.ReadAsStringAsync();
                var userResponse = System.Text.Json.JsonSerializer.Deserialize<UserResponse>(
                    content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (userResponse != null && userResponse.Id != Guid.Empty)
                {
                    CurrentUserId = userResponse.Id;
                }
                else
                {
                    CurrentUserId = GetFallbackUserId();
                }
            }
            else
            {
                CurrentUserId = GetFallbackUserId();
            }
        }
        catch
        {
            // Use fallback user ID if API call fails (expected until authentication is implemented)
            CurrentUserId = GetFallbackUserId();
        }
    }

    private record UserResponse(Guid Id);

    private static Guid GetFallbackUserId()
    {
        // Fallback user ID to use until authentication is properly implemented
        return Guid.Parse("8dc55ac3-5e02-49fb-867e-7aa82d3ca8bc");
    }

    protected async Task OnValidSubmit(EditContext editContext)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Model.Location))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validation Failed",
                    Detail = "Location is required. Please select a location from the suggestions.",
                    Duration = 5000
                });
                return;
            }

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

                if (release.Price < 0)
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
                PriceCents: (int)Math.Round(tt.Price * 100m, MidpointRounding.AwayFromZero),
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

    protected void OnFileChange(InputFileChangeEventArgs e)
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

    protected async Task OnLocationChange(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 3)
        {
            LocationSuggestions.Clear();
            return;
        }

        IsSearchingLocations = true;
        try
        {
            LocationSuggestions = (await PlacesClient.SearchAsync(value)).ToList();
        }
        catch
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Location Search Error",
                Detail = "Could not fetch location suggestions. You can still enter the location manually.",
                Duration = 5000
            });
        }
        finally
        {
            IsSearchingLocations = false;
        }
    }

    protected async Task OnLocationChangeHandler(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            await OnLocationChange("");
        }
        else
        {
            await OnLocationChange(value);
        }
    }

    protected async Task OnLoadLocationData(LoadDataArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Filter) || args.Filter.Length < 3)
        {
            LocationSuggestions.Clear();
            return;
        }

        await OnLocationChange(args.Filter);
    }

    protected async Task OnLocationSelected(object value)
    {
        if (value is null)
        {
            return;
        }

        var selectedMainText = value.ToString();
        var prediction = LocationSuggestions.FirstOrDefault(p =>
            string.Equals(p.MainText, selectedMainText, StringComparison.Ordinal));

        if (prediction is null)
        {
            return;
        }

        try
        {
            var details = await PlacesClient.GetDetailsAsync(prediction.PlaceId);
            Model.Location = BuildFullAddress(prediction, details);
        }
        catch
        {
            Model.Location = BuildFullAddress(prediction, null);
        }
    }

    private static string BuildFullAddress(PlacePredictionDto prediction, PlaceDetailsDto? details)
    {
        if (details is null)
        {
            var fallback = prediction.MainText;
            if (!string.IsNullOrEmpty(prediction.SecondaryText))
            {
                fallback += ", " + prediction.SecondaryText;
            }
            return fallback;
        }

        var name = !string.IsNullOrWhiteSpace(details.Name) ? details.Name : prediction.MainText;
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(name))
        {
            parts.Add(name!);
        }

        if (!string.IsNullOrWhiteSpace(details.StreetAddress) &&
            !string.Equals(details.StreetAddress, name, StringComparison.OrdinalIgnoreCase))
        {
            parts.Add(details.StreetAddress!);
        }

        if (!string.IsNullOrWhiteSpace(details.PostalCode))
        {
            parts.Add(details.PostalCode!);
        }

        if (!string.IsNullOrWhiteSpace(details.City))
        {
            parts.Add(details.City!);
        }

        if (!string.IsNullOrWhiteSpace(details.Country))
        {
            parts.Add(details.Country!);
        }

        if (parts.Count == 0)
        {
            return details.FormattedAddress ?? prediction.MainText;
        }

        return string.Join(", ", parts);
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
    [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
    public decimal Price { get; set; } = 0m;

    [Required]
    [StringLength(3, ErrorMessage = "Currency must be 3 characters.")]
    public string Currency { get; set; } = "EUR";

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Max uses must be at least 1.")]
    public int MaxUses { get; set; } = 1;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; } = 100;
}
