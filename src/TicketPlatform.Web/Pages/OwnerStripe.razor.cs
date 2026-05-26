using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Pages;

public class OwnerStripeBase : ComponentBase
{
    [Inject] protected HttpClient Http { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;

    [Parameter] public Guid HostId { get; set; }

    protected StripeConnectStatusDto? Status { get; set; }
    protected bool Loading { get; set; }
    protected bool _connecting;
    protected string? Error { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadStatus();
    }

    protected async Task LoadStatus()
    {
        Loading = true;
        Error = null;

        try
        {
            Status = await Http.GetFromJsonAsync<StripeConnectStatusDto>(
                $"api/stripe-connect/status/{HostId}");
        }
        catch (Exception ex)
        {
            Error = $"Failed to load Stripe status: {ex.Message}";
        }
        finally
        {
            Loading = false;
        }
    }

    protected async Task ConnectStripe()
    {
        _connecting = true;
        Error = null;

        try
        {
            var response = await Http.PostAsync(
                $"api/stripe-connect/onboard/{HostId}",
                null);

            if (!response.IsSuccessStatusCode)
            {
                Error = $"Could not start Stripe onboarding: {await response.Content.ReadAsStringAsync()}";
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<StripeConnectLinkResponse>();

            if (!string.IsNullOrWhiteSpace(result?.Url))
                Nav.NavigateTo(result.Url, forceLoad: true);
        }
        catch (Exception ex)
        {
            Error = $"Could not start Stripe onboarding: {ex.Message}";
        }
        finally
        {
            _connecting = false;
        }
    }

    protected sealed record StripeConnectLinkResponse(string Url);
}
