using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public class CheckoutBase : ComponentBase
{
    [Inject] protected HttpClient Http { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] protected NotificationService Notify { get; set; } = null!;
    [Inject] protected AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private IUserSettingsClient SettingsClient { get; set; } = null!;

    [Parameter] public Guid EventId { get; set; }

    protected string FirstName { get; set; } = string.Empty;
    protected string LastName { get; set; } = string.Empty;
    protected string Email { get; set; } = string.Empty;
    protected bool RemindersEnabled { get; set; } = true;
    protected bool IsAuthenticated { get; private set; }

    private Dictionary<Guid, int> Cart { get; } = new();

    protected EventDto? EventDetails { get; private set; }
    protected bool EventLoadFailed { get; private set; }
    protected bool Busy { get; private set; }
    protected bool ShowDetails { get; private set; }

    protected string? EventTitle => EventDetails?.Title;

    protected bool CartHasItems => Cart.Values.Any(q => q > 0);

    protected int TotalCents =>
        EventDetails?.TicketTypes
            .Where(tt => Cart.TryGetValue(tt.Id, out var q) && q > 0)
            .Sum(tt => tt.PriceCents * Cart[tt.Id]) ?? 0;

    protected int Remaining(TicketTypeDto tt) => tt.Quantity - tt.Sold;

    protected int GetQuantity(Guid id) => Cart.GetValueOrDefault(id, 0);

    protected override async Task OnParametersSetAsync() => await LoadEventAsync();

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthStateProvider.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
        {
            IsAuthenticated = true;

            var profile = await SettingsClient.GetAsync();
            FirstName = profile?.FirstName ?? string.Empty;
            LastName  = profile?.LastName  ?? string.Empty;
            Email     = profile?.Email ?? string.Empty;
        }
    }

    private async Task LoadEventAsync()
    {
        try
        {
            EventDetails = await Http.GetFromJsonAsync<EventDto>($"api/events/{EventId}");

            if (EventDetails is null)
            {
                EventLoadFailed = true;
                Notify.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Event not found",
                    Detail = "This event does not exist or is no longer available.",
                    Duration = 6000
                });
            }
            else
            {
                foreach (var tt in EventDetails.TicketTypes)
                    Cart[tt.Id] = 0;
            }
        }
        catch (Exception ex)
        {
            EventLoadFailed = true;
            Notify.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Failed to load event",
                Detail = ex.Message,
                Duration = 5000
            });
        }
    }

    protected void SetQuantity(Guid id, int value)
    {
        var tt = EventDetails?.TicketTypes.FirstOrDefault(t => t.Id == id);
        if (tt is null) return;

        Cart[id] = Math.Clamp(value, 0, Remaining(tt));
    }

    protected void ProceedToDetails()
    {
        if (!CartHasItems)
        {
            Notify.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Empty cart",
                Detail = "Please add at least one ticket before continuing.",
                Duration = 4000
            });
            return;
        }

        ShowDetails = true;
    }

    protected void GoBackToSelection() => ShowDetails = false;

    protected async Task HandleCheckout()
    {
        if (!CartHasItems) return;

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            Notify.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Missing name",
                Detail = "Please enter both your first and last name.",
                Duration = 4000
            });
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@') || !Email.Contains('.'))
        {
            Notify.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Invalid email",
                Detail = "Please enter a valid email address.",
                Duration = 4000
            });
            return;
        }

        Busy = true;

        try
        {
            var items = Cart
                .Where(kv => kv.Value > 0)
                .Select(kv => new TicketTypeQuantityDto(kv.Key, kv.Value))
                .ToList();

            var request = new CheckoutRequestDto(
                Items: items,
                Email: Email.Trim(),
                FirstName: FirstName.Trim(),
                LastName: LastName.Trim(),
                RemindersEnabled: RemindersEnabled
            );

            var response = await Http.PostAsJsonAsync("api/tickets/checkout", request);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode == 409)
                {
                    Notify.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary  = "Tickets no longer available",
                        Detail   = string.IsNullOrWhiteSpace(detail)
                            ? "Someone else just bought the last ticket(s). Please update your selection."
                            : detail,
                        Duration = 8000
                    });

                    // Reload so Remaining() shows current stock, send back to ticket selection
                    await LoadEventAsync();
                    ShowDetails = false;
                    return;
                }

                Notify.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary  = "Checkout failed",
                    Detail   = string.IsNullOrWhiteSpace(detail)
                        ? $"Server returned {(int)response.StatusCode}."
                        : detail,
                    Duration = 6000
                });
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<CheckoutResponseDto>();

            if (result is null || string.IsNullOrWhiteSpace(result.CheckoutUrl))
            {
                Notify.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Checkout failed",
                    Detail = "The server did not return a valid Stripe checkout URL.",
                    Duration = 6000
                });
                return;
            }

            Nav.NavigateTo(result.CheckoutUrl, forceLoad: true);
        }
        catch (Exception ex)
        {
            Notify.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Unexpected error",
                Detail = ex.Message,
                Duration = 5000
            });
        }
        finally
        {
            Busy = false;
        }
    }

    protected string FormatPrice(int cents, string currency = "USD") =>
        cents == 0
            ? "Free"
            : (cents / 100m).ToString($"0.00 {currency.ToUpper()}",
                System.Globalization.CultureInfo.InvariantCulture);

    protected static DateTimeOffset StartDate(EventDto e) =>
        e.TicketTypes.Min(tt => tt.OccurenceStartDate).ToLocalTime();

    protected static DateTimeOffset EndDate(EventDto e) =>
        e.TicketTypes.Max(tt => tt.OccurenceEndDate).ToLocalTime();
}
