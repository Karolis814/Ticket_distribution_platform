using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using TicketPlatform.Shared;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public class EventsBase : ComponentBase
{
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private NotificationService Notify { get; set; } = null!;
    [Inject] private HttpClient Http { get; set; } = null!;
    [Inject] protected IEventsClient EventsClient { get; set; } = default!;

    protected PagedResult<EventDto>? Result { get; private set; }
    private int Page { get; set; } = 1;
    protected const int PageSize = 20;

    protected IReadOnlyList<EventDto> Events { get; private set; } = [];
    protected IReadOnlyList<string> LocationSuggestions { get; private set; } = [];

    protected IReadOnlyList<string> TitleSuggestions { get; private set; } = [];

    protected bool IsLoading { get; private set; }

    protected string SearchText { get; set; } = string.Empty;
    protected string LocationText { get; set; } = string.Empty;
    protected DateTimeOffset? FromDate { get; set; }

    protected IReadOnlyList<string> Categories { get; set; } = [];
    protected string? SelectedCategory { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Categories = await EventsClient.GetCategoriesAsync();

        await LoadEventsAsync();
    }

    private async Task LoadEventsAsync()
    {
        IsLoading = true;

        try
        {
            Result = await Http.GetFromJsonAsync<PagedResult<EventDto>>(
                $"api/events?page={Page}&pageSize={PageSize}");

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

    protected async Task ApplyFilterAsync()
    {
        IsLoading = true;

        try
        {
            Events = await EventsClient.SearchAsync(
                SearchText,
                FromDate,
                LocationText,
                SelectedCategory);
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

    protected async Task ClearFilterAsync()
    {
        SearchText = string.Empty;
        LocationText = string.Empty;
        FromDate = null;
        SelectedCategory = null;
        LocationSuggestions = [];

        Page = 1;
        await LoadEventsAsync();
    }

    protected void OnSearchInput(ChangeEventArgs e)
    {
        SearchText = e.Value?.ToString() ?? string.Empty;
    }

    protected async Task LoadLocationSuggestions(LoadDataArgs args)
    {
        var input = args.Filter ?? string.Empty;

        LocationText = input;

        LocationSuggestions =
            await EventsClient.GetLocationSuggestionsAsync(input);
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

    protected void NavigateToDetails(Guid eventId)
    {
        Nav.NavigateTo($"/events/{eventId}");
    }

    protected static int RemainingTickets(EventDto ev)
    {
        var total = ev.TicketTypes.Sum(t => t.Quantity);
        var sold = ev.TicketTypes.Sum(t => t.Sold);

        return Math.Max(0, total - sold);
    }

    protected static DateTimeOffset StartDate(EventDto e) =>
        e.TicketTypes.Min(tt => tt.OccurenceStartDate);

    protected static DateTimeOffset EndDate(EventDto e) =>
        e.TicketTypes.Max(tt => tt.OccurenceEndDate);

    protected static string GetStartingPriceText(EventDto ev)
    {
        if (ev.TicketTypes.Count == 0)
            return "Get Tickets";

        var minPriceTicket =
            ev.TicketTypes.OrderBy(t => t.PriceCents).First();

        return
            $"Starting from {FormatPrice(minPriceTicket.PriceCents, minPriceTicket.Currency)}";
    }

    private static string FormatPrice(
        int cents,
        string currency = "USD") =>
        cents == 0
            ? "Free"
            : (cents / 100m).ToString(
                $"0.00 {currency.ToUpper()}",
                System.Globalization.CultureInfo.InvariantCulture);

    protected static string GetDescriptionExcerpt(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        return description.Length > 100
            ? $"{description[..97]}..."
            : description;
    }

    protected async Task LoadTitleSuggestions(LoadDataArgs args)
    {
        var input = args.Filter ?? string.Empty;

        if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
        {
            TitleSuggestions = [];
            return;
        }

        var events = await EventsClient.SearchAsync(
            input,
            null,
            null,
            SelectedCategory);

        TitleSuggestions = events
            .Select(e => e.Title)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .OrderBy(t => t)
            .Take(10)
            .ToList();
    }
}
