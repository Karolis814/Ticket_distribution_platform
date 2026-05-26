using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Pages;

public class OwnerDashboardBase : ComponentBase
{
    [Inject] private HttpClient Http { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;

    protected OwnerDashboardDto? Dashboard { get; set; }
    protected bool Loading { get; set; } = true;
    protected string? Error { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        var userIdStr = authState.User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdStr, out var hostId))
        {
            Error = "Could not determine your user identity.";
            Loading = false;
            return;
        }

        try
        {
            Dashboard = await Http.GetFromJsonAsync<OwnerDashboardDto>($"api/owner-dashboard/{hostId}");
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            Loading = false;
        }
    }

    protected static string FormatMoney(MoneyAmountDto amount)
        => $"{amount.AmountCents / 100m:0.00} {amount.Currency.ToUpper()}";
}
