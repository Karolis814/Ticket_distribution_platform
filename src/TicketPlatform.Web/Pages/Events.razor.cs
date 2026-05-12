using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Radzen;
using TicketPlatform.Shared;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Web.Pages;

public class EventsBase : ComponentBase
{
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private NotificationService Notify { get; set; } = null!;
    [Inject] private HttpClient Http { get; set; } = null!;

    protected PagedResult<EventDto>? Result { get; private set; }
    private int Page { get; set; } = 1;
    protected const int PageSize = 20;

    protected IReadOnlyList<EventDto> Events { get; private set; } = [];
    protected bool IsLoading { get; private set; }

    protected static int RemainingTickets(EventDto ev)
    {
        var total = ev.TicketTypes.Sum(t => t.Quantity);
        var sold = ev.TicketTypes.Sum(t => t.Sold);
        return Math.Max(0, total - sold);
    }

    protected static DateTimeOffset StartDate(EventDto e) => e.TicketTypes.Min(tt => tt.OccurenceStartDate);
    protected static DateTimeOffset EndDate(EventDto e) => e.TicketTypes.Max(tt => tt.OccurenceEndDate);

    protected override async Task OnInitializedAsync() => await LoadEventsAsync();

    private async Task LoadEventsAsync()
    {
        IsLoading = true;
        try
        {
            Result = await Http.GetFromJsonAsync<PagedResult<EventDto>>($"api/events?page={Page}&pageSize={PageSize}");
            Events = Result?.Items ?? [];
        }
        catch (Exception ex)
        {
            Notify.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Failed to load events",
                Detail = ex.Message,
                Duration = 5000
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task OnPageChange(PagerEventArgs args)
    {
        Page = args.PageIndex + 1;
        await LoadEventsAsync();
    }

    protected void NavigateToCheckout(Guid eventId)
    {
        Nav.NavigateTo($"/checkout/{eventId}");
    }

    protected static string GetStartingPriceText(EventDto ev)
    {
        if (ev.TicketTypes.Count == 0) return "Get Tickets";
        var minPriceTicket = ev.TicketTypes.OrderBy(t => t.PriceCents).First();
        return $"Starting from {FormatPrice(minPriceTicket.PriceCents, minPriceTicket.Currency)}";
    }

    private static string FormatPrice(int cents, string currency = "USD") =>
        cents == 0
            ? "Free"
            : (cents / 100m).ToString($"0.00 {currency.ToUpper()}", System.Globalization.CultureInfo.InvariantCulture);

    protected static string GetDescriptionExcerpt(string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return string.Empty;
        return description.Length > 100 ? $"{description[..97]}..." : description;
    }
}
