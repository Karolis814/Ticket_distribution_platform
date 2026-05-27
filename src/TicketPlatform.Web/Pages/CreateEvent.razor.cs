using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public partial class CreateEventBase : ComponentBase
{
    protected CreateEventFormModel Model { get; set; } = new()
    {
        Status = EventStatus.Published,
        TicketReleases = [new TicketReleaseModel()]
    };

    protected List<EventCategory> Categories { get; set; } =
        Enum.GetValues<EventCategory>().ToList();

    protected List<EventStatus> EventStatuses { get; set; } =
        Enum.GetValues<EventStatus>().ToList();

    protected List<string> Currencies { get; set; } =
    [
        "EUR",
        "USD",
        "GBP"
    ];

    [Inject] protected IEventsClient EventsClient { get; set; } = null!;
    [Inject] protected IPlacesClient PlacesClient { get; set; } = null!;
    [Inject] protected IUsersClient UsersClient { get; set; } = null!;
    [Inject] protected IHostPaymentsClient HostPaymentsClient { get; set; } = null!;
    [Inject] protected IImagesClient ImagesClient { get; set; } = null!;
    [Inject] protected NotificationService NotificationService { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    [Inject] protected AuthenticationStateProvider AuthState { get; set; } = null!;

    private Guid CurrentUserId { get; set; } = Guid.Empty;
    protected bool IsInitialized { get; set; } = false;
    protected bool IsUploadingImage { get; set; } = false;

    protected bool IsFormValid =>
        !string.IsNullOrWhiteSpace(Model.Title) &&
        !string.IsNullOrWhiteSpace(Model.Description) &&
        Model.Category.HasValue &&
        !string.IsNullOrWhiteSpace(Model.Location) &&
        Model.TicketReleases.Count > 0 &&
        Model.TicketReleases.All(r =>
            !string.IsNullOrWhiteSpace(r.Title) &&
            !string.IsNullOrWhiteSpace(r.Currency));
    protected List<PlacePredictionDto> LocationSuggestions { get; set; } = [];
    protected bool IsSearchingLocations { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthState.GetAuthenticationStateAsync();
        if (!auth.User.IsInRole("Host"))
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Connect Stripe first",
                Detail = "Complete Stripe onboarding in Settings to start hosting events.",
                Duration = 7000
            });
            NavigationManager.NavigateTo("/user/settings");
            return;
        }

        await GetCurrentUserIdAsync();

        var stripeStatus = await HostPaymentsClient.GetStatusAsync(CurrentUserId);

        if (stripeStatus is null || !stripeStatus.Ready)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Stripe setup required",
                Detail = "Connect Stripe before creating events.",
                Duration = 7000
            });

            NavigationManager.NavigateTo("/user/settings");
            return;
        }

        IsInitialized = true;
    }

    private async Task GetCurrentUserIdAsync()
    {
        CurrentUserId = await UsersClient.GetCurrentUserIdAsync();
    }

    protected async Task OnValidSubmit(EditContext editContext)
    {
        try
        {
            if (!Model.UseIndividualDates)
            {
                if (Model.OccurenceEndDate <= Model.OccurenceStartDate)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Validation Failed",
                        Detail = "Event end time must be after the start time.",
                        Duration = 5000
                    });
                    return;
                }

                foreach (var release in Model.TicketReleases)
                {
                    release.OccurenceStartDate = Model.OccurenceStartDate;
                    release.OccurenceEndDate   = Model.OccurenceEndDate;
                }
            }

            foreach (var release in Model.TicketReleases)
            {
                if (!release.UseCustomAdmissionTimes)
                {
                    release.AdmissionStartDate = release.OccurenceStartDate;
                    release.AdmissionEndDate   = release.OccurenceEndDate;
                }

                if (!release.UseMaxUses)
                    release.MaxUses = 1;
            }

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
                EventId: Guid.Empty,
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
                Category: Model.Category!.Value,
                Title: Model.Title,
                Description: Model.Description,
                Location: Model.Location,
                ThumbnailUrl: Model.ThumbnailUrl,
                Status: Model.Status,
                TicketTypes: ticketTypes
            );

            var created = await EventsClient.CreateAsync(createRequest);

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Event Created",
                Detail = $"Event '{Model.Title}' has been successfully created!",
                Duration = 5000
            });

            NavigationManager.NavigateTo($"/events/{created!.Id}");
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

    protected void SyncCurrency(string currency)
    {
        foreach (var release in Model.TicketReleases)
            release.Currency = currency;
    }

    protected void AddTicketRelease()
    {
        var currency = Model.TicketReleases.FirstOrDefault()?.Currency ?? "EUR";
        Model.TicketReleases.Add(new TicketReleaseModel { Currency = currency });
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
        var file = e.GetMultipleFiles(1).FirstOrDefault();
        if (file is null) return;

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Unsupported file type",
                Detail = "Please upload a JPEG, PNG, or WebP image.",
                Duration = 5000
            });
            return;
        }

        IsUploadingImage = true;
        StateHasChanged();

        try
        {
            await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            var response = await ImagesClient.UploadEventThumbnailAsync(stream, file.Name, file.ContentType);
            Model.ThumbnailUrl = response.Url;
            Model.ThumbnailFileName = file.Name;

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Image uploaded",
                Detail = $"'{file.Name}' uploaded successfully.",
                Duration = 3000
            });
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Upload failed",
                Detail = ex.Message,
                Duration = 5000
            });
        }
        finally
        {
            IsUploadingImage = false;
        }
    }

    private async Task OnLocationChange(string value)
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

    protected async Task OnLocationSelected(object? value)
    {
        if (value is null)
            return;

        var selectedMainText = value.ToString();

        var prediction = LocationSuggestions.FirstOrDefault(p =>
            string.Equals(p.MainText, selectedMainText, StringComparison.Ordinal));

        if (prediction is null)
            return;

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

    private static string BuildFullAddress(
        PlacePredictionDto prediction,
        PlaceDetailsDto? details)
    {
        if (details is null)
        {
            var fallback = prediction.MainText;

            if (!string.IsNullOrEmpty(prediction.SecondaryText))
                fallback += ", " + prediction.SecondaryText;

            return fallback;
        }

        var name = !string.IsNullOrWhiteSpace(details.Name)
            ? details.Name
            : prediction.MainText;

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(name))
            parts.Add(name!);

        if (!string.IsNullOrWhiteSpace(details.StreetAddress) &&
            !string.Equals(details.StreetAddress, name, StringComparison.OrdinalIgnoreCase))
        {
            parts.Add(details.StreetAddress!);
        }

        if (!string.IsNullOrWhiteSpace(details.PostalCode))
            parts.Add(details.PostalCode!);

        if (!string.IsNullOrWhiteSpace(details.City))
            parts.Add(details.City!);

        if (!string.IsNullOrWhiteSpace(details.Country))
            parts.Add(details.Country!);

        if (parts.Count == 0)
            return details.FormattedAddress ?? prediction.MainText;

        return string.Join(", ", parts);
    }
}

public class CreateEventFormModel
{
    [Required]
    public EventCategory? Category { get; set; } = null;

    [Required]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(10000, ErrorMessage = "Description cannot exceed 10000 characters.")]
    public string Description { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Location cannot exceed 300 characters.")]
    public string? Location { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Published;

    public DateTimeOffset OccurenceStartDate { get; set; } =
        DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(12);

    public DateTimeOffset OccurenceEndDate { get; set; } =
        DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(14);

    public bool UseIndividualDates { get; set; } = false;

    public string? ThumbnailUrl { get; set; }

    public string? ThumbnailFileName { get; set; } = string.Empty;

    public List<TicketReleaseModel> TicketReleases { get; set; } = [];
}

public class TicketReleaseModel
{
    [Required]
    [StringLength(100, ErrorMessage = "Ticket type title cannot exceed 100 characters.")]
    public string Title { get; set; } = "Standard Ticket";

    public bool UseCustomAdmissionTimes { get; set; } = false;

    public bool UseMaxUses { get; set; } = false;

    [Required]
    public DateTimeOffset OccurenceStartDate { get; set; } =
        DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(12);

    [Required]
    public DateTimeOffset OccurenceEndDate { get; set; } =
        DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(14);

    [Required]
    public DateTimeOffset AdmissionStartDate { get; set; } =
        DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(12);

    [Required]
    public DateTimeOffset AdmissionEndDate { get; set; } =
        DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(14);

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
    public int Quantity { get; set; } = 1;
}
