using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
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
    [Parameter] public Guid? EventId { get; set; }
    protected bool IsEditMode => EventId.HasValue;

    protected CreateEventFormModel Model { get; set; } = new()
    {
        Status = EventStatus.Published,
        TicketReleases = [new TicketReleaseModel()]
    };

    protected List<EventCategory> Categories { get; set; } =
        Enum.GetValues<EventCategory>().ToList();

    protected bool IsCancelConfirming { get; set; }
    protected bool IsCancelled { get; private set; }

    protected List<string> Currencies { get; set; } =
    [
        "EUR",
        "USD",
        "GBP"
    ];

    [Inject] protected HttpClient Http { get; set; } = null!;
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
    protected decimal PlatformFeePercent { get; private set; } = 5m;
    protected bool IsUploadingImage { get; set; } = false;
    private bool _firstParametersSet = true;

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

    protected override async Task OnParametersSetAsync()
    {
        if (_firstParametersSet)
        {
            _firstParametersSet = false;
            return;
        }

        Model = new CreateEventFormModel
        {
            Status = EventStatus.Published,
            TicketReleases = [new TicketReleaseModel()]
        };
        IsCancelConfirming = false;
        IsInitialized = false;

        if (IsEditMode)
        {
            var existing = await EventsClient.GetByIdAsync(EventId!.Value);
            if (existing is null || existing.HostId != CurrentUserId)
            {
                NavigationManager.NavigateTo("/host/events");
                return;
            }
            PopulateModel(existing);
        }

        IsInitialized = true;
    }

    protected override async Task OnInitializedAsync()
    {
        var feeResult = await Http.GetFromJsonAsync<FeeDto>("api/platform/fee");
        PlatformFeePercent = feeResult?.FeePercent ?? 5m;

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

        if (IsEditMode)
        {
            var existing = await EventsClient.GetByIdAsync(EventId!.Value);
            if (existing is null || existing.HostId != CurrentUserId)
            {
                NavigationManager.NavigateTo("/host/events");
                return;
            }

            PopulateModel(existing);
        }

        IsInitialized = true;
    }

    private void PopulateModel(EventDto dto)
    {
        IsCancelled = dto.Status == EventStatus.Cancelled;
        Model.Category = dto.Category;
        Model.Title = dto.Title;
        Model.Description = dto.Description;
        Model.Location = dto.Location;
        Model.ThumbnailUrl = dto.ThumbnailUrl;
        Model.Status = dto.Status;

        Model.TicketReleases = dto.TicketTypes.Select(tt => new TicketReleaseModel
        {
            Id = tt.Id,
            Title = tt.Title,
            OccurenceStartDate = tt.OccurenceStartDate.ToLocalTime(),
            OccurenceEndDate = tt.OccurenceEndDate.ToLocalTime(),
            AdmissionStartDate = tt.AdmissionStartDate.ToLocalTime(),
            AdmissionEndDate = tt.AdmissionEndDate.ToLocalTime(),
            Price = tt.PriceCents / 100m,
            Currency = tt.Currency,
            MaxUses = tt.MaxUses,
            UseMaxUses = tt.MaxUses > 1,
            UseCustomAdmissionTimes =
                tt.AdmissionStartDate != tt.OccurenceStartDate ||
                tt.AdmissionEndDate != tt.OccurenceEndDate,
            Quantity = (int?)tt.Quantity
        }).ToList();

        if (Model.TicketReleases.Count > 0)
        {
            var first = Model.TicketReleases[0];
            Model.UseIndividualDates = !Model.TicketReleases.All(r =>
                r.OccurenceStartDate == first.OccurenceStartDate &&
                r.OccurenceEndDate == first.OccurenceEndDate);

            if (!Model.UseIndividualDates)
            {
                Model.OccurenceStartDate = first.OccurenceStartDate;
                Model.OccurenceEndDate = first.OccurenceEndDate;
            }
        }
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

                if ((release.Quantity ?? 0) < 1)
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

                var price = release.Price ?? 0m;
                if (price < 0 || (price > 0 && price < 0.5m))
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Validation Failed",
                        Detail = $"Release '{release.Title}': Price must be 0 or at least 0.50.",
                        Duration = 5000
                    });
                    return;
                }
            }

            if (IsEditMode)
            {
                var ticketTypes = Model.TicketReleases.Select(tt => new UpdateTicketTypeRequest(
                    Id: tt.Id,
                    Title: tt.Title,
                    OccurenceStartDate: tt.OccurenceStartDate,
                    OccurenceEndDate: tt.OccurenceEndDate,
                    AdmissionStartDate: tt.AdmissionStartDate,
                    AdmissionEndDate: tt.AdmissionEndDate,
                    PriceCents: (int)Math.Round((tt.Price ?? 0m) * 100m, MidpointRounding.AwayFromZero),
                    Currency: tt.Currency,
                    MaxUses: tt.MaxUses,
                    Quantity: tt.Quantity ?? 1
                )).ToList();

                var updateRequest = new UpdateEventRequest(
                    Category: Model.Category!.Value,
                    Title: Model.Title,
                    Description: Model.Description,
                    Location: Model.Location,
                    ThumbnailUrl: Model.ThumbnailUrl,
                    Status: IsCancelled ? EventStatus.Cancelled : Model.Status,
                    TicketTypes: ticketTypes
                );

                await EventsClient.UpdateAsync(EventId!.Value, updateRequest);

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Event Updated",
                    Detail = $"'{Model.Title}' has been updated.",
                    Duration = 5000
                });

                NavigationManager.NavigateTo($"/events/{EventId!.Value}");
            }
            else
            {
                var ticketTypes = Model.TicketReleases.Select(tt => new CreateTicketTypeRequest(
                    EventId: Guid.Empty,
                    Title: tt.Title,
                    OccurenceStartDate: tt.OccurenceStartDate,
                    OccurenceEndDate: tt.OccurenceEndDate,
                    AdmissionStartDate: tt.AdmissionStartDate,
                    AdmissionEndDate: tt.AdmissionEndDate,
                    PriceCents: (int)Math.Round((tt.Price ?? 0m) * 100m, MidpointRounding.AwayFromZero),
                    Currency: tt.Currency,
                    MaxUses: tt.MaxUses,
                    Quantity: tt.Quantity ?? 1
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
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = IsEditMode ? "Failed to update event" : "Failed to create event",
                Detail = ex.Message,
                Duration = 5000
            });
        }
    }

    protected async Task CancelEventAsync()
    {
        try
        {
            var request = new UpdateEventRequest(
                Category: Model.Category!.Value,
                Title: Model.Title,
                Description: Model.Description,
                Location: Model.Location,
                ThumbnailUrl: Model.ThumbnailUrl,
                Status: EventStatus.Cancelled,
                TicketTypes: Model.TicketReleases.Select(tt => new UpdateTicketTypeRequest(
                    Id: tt.Id,
                    Title: tt.Title,
                    OccurenceStartDate: tt.OccurenceStartDate,
                    OccurenceEndDate: tt.OccurenceEndDate,
                    AdmissionStartDate: tt.AdmissionStartDate,
                    AdmissionEndDate: tt.AdmissionEndDate,
                    PriceCents: (int)Math.Round((tt.Price ?? 0m) * 100m, MidpointRounding.AwayFromZero),
                    Currency: tt.Currency,
                    MaxUses: tt.MaxUses,
                    Quantity: tt.Quantity ?? 1
                )).ToList()
            );

            await EventsClient.UpdateAsync(EventId!.Value, request);
            Model.Status = EventStatus.Cancelled;

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Event cancelled",
                Detail = $"'{Model.Title}' has been cancelled.",
                Duration = 5000
            });

            NavigationManager.NavigateTo($"/events/{EventId!.Value}");
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Failed to cancel event",
                Detail = ex.Message,
                Duration = 5000
            });
            IsCancelConfirming = false;
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

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/heic", "image/heif" };
        if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Unsupported file type",
                Detail = "Please upload a JPEG, PNG, WebP, or HEIC image.",
                Duration = 5000
            });
            return;
        }

        IsUploadingImage = true;
        StateHasChanged();

        try
        {
            await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            var uploadName = Path.GetFileNameWithoutExtension(file.Name) + ".jpg";
            var response = await ImagesClient.UploadEventThumbnailAsync(stream, uploadName, file.ContentType);
            Model.ThumbnailUrl = response.Url;
            Model.ThumbnailFileName = uploadName;

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Image uploaded",
                Detail = $"{file.Name}",
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

    protected string NetAmount(int index)
    {
        var price = Model.TicketReleases[index].Price ?? 0m;
        return (price * (1 - PlatformFeePercent / 100m)).ToString("0.00");
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
    public Guid? Id { get; set; }

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
    public decimal? Price { get; set; } = null;

    [Required]
    [StringLength(3, ErrorMessage = "Currency must be 3 characters.")]
    public string Currency { get; set; } = "EUR";

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Max uses cannot be negative.")]
    public int MaxUses { get; set; } = 1;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int? Quantity { get; set; } = null;
}
