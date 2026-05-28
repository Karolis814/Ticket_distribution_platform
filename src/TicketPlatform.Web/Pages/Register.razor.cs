using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;
using TicketPlatform.Web.Services;
using TicketPlatform.Shared.Dtos;
using Microsoft.AspNetCore.Components.Authorization;

namespace TicketPlatform.Web.Pages;

public class RegisterBase : ComponentBase
{
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] protected IAuthClient AuthClient { get; set; } = null!;
    [Inject] protected IPlacesClient PlacesClient { get; set; } = null!;

    protected readonly UserSignUpDTO Model = new();
    protected string ConfirmPassword { get; set; } = "";
    protected EditContext EditContext = default!;
    protected string? ErrorMessage;
    protected bool IsLoading;
    protected List<PlacePredictionDto> AddressSuggestions { get; set; } = [];
    protected bool IsSearchingAddress { get; set; }

    protected override Task OnInitializedAsync()
    {
        EditContext = new EditContext(Model);
        return Task.CompletedTask;
    }

    protected async Task HandleRegister()
    {
        ErrorMessage = null;

        if (!EditContext.Validate())
            return;

        if (Model.Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        IsLoading = true;

        var (success, error) = await AuthClient.RegisterAsync(Model);

        if (success)
            Nav.NavigateTo("/events");
        else
            ErrorMessage = error;

        IsLoading = false;
    }

    protected async Task OnLoadAddressData(LoadDataArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Filter) || args.Filter.Length < 3)
        {
            AddressSuggestions.Clear();
            return;
        }

        IsSearchingAddress = true;
        StateHasChanged();
        try
        {
            AddressSuggestions = (await PlacesClient.SearchAsync(args.Filter)).ToList();
        }
        finally
        {
            IsSearchingAddress = false;
        }
    }

    protected async Task OnAddressSelected(object? value)
    {
        if (value?.ToString() is not string selected) return;

        var prediction = AddressSuggestions.FirstOrDefault(p =>
            string.Equals(p.MainText, selected, StringComparison.Ordinal));

        if (prediction is null) return;

        try
        {
            var details = await PlacesClient.GetDetailsAsync(prediction.PlaceId);
            Model.Address = details?.FormattedAddress ?? FallbackAddress(prediction);
        }
        catch
        {
            Model.Address = FallbackAddress(prediction);
        }
    }

    private static string FallbackAddress(PlacePredictionDto p) =>
        string.IsNullOrEmpty(p.SecondaryText) ? p.MainText : $"{p.MainText}, {p.SecondaryText}";
}
