using Microsoft.AspNetCore.Components;
using Radzen;
using TicketPlatform.Shared;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public class EventsBase : ComponentBase
{
    protected const int PageSize = 20;
    [Inject] protected IEventsClient EventsClient { get; set; } = null!;

    protected PagedResult<EventDto>? Result { get; private set; }
    private int Page { get; set; } = 1;

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
            Result = await EventsClient.GetPagedAsync(Page, PageSize,
                SearchText, FromDate, LocationText, SelectedCategory);
            Events = Result?.Items ?? [];
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task ApplyFilterAsync()
    {
        Page = 1;
        await LoadEventsAsync();
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
        LocationSuggestions = await EventsClient.GetLocationSuggestionsAsync(input);
    }

    protected async Task OnPageChange(PagerEventArgs args)
    {
        Page = args.PageIndex + 1;
        await LoadEventsAsync();
    }

    protected async Task LoadTitleSuggestions(LoadDataArgs args)
    {
        var input = args.Filter ?? string.Empty;

        if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
        {
            TitleSuggestions = [];
            return;
        }

        var result = await EventsClient.GetPagedAsync(1, 50, title: input, category: SelectedCategory);

        TitleSuggestions = (result?.Items ?? [])
            .Select(e => e.Title)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .OrderBy(t => t)
            .Take(10)
            .ToList();
    }
}
