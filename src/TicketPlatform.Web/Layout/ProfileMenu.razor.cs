using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

namespace TicketPlatform.Web.Layout;

public class ProfileMenuBase : ComponentBase, IDisposable
{
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    protected bool IsAuthenticated;
    protected bool IsHost;
    protected string Email = "";

    protected override async Task OnInitializedAsync()
    {
        AuthState.AuthenticationStateChanged += OnAuthStateChanged;
        await UpdateAuthState();
    }

    private async void OnAuthStateChanged(Task<AuthenticationState> _)
    {
        try
        {
            await UpdateAuthState();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Authentication error", e.Message);
        }
    }

    private async Task UpdateAuthState()
    {
        var state = await AuthState.GetAuthenticationStateAsync();
        IsAuthenticated = state.User.Identity?.IsAuthenticated ?? false;
        IsHost = IsAuthenticated && state.User.IsInRole("Host");
        Email = IsAuthenticated
            ? state.User.FindFirst("email")?.Value
              ?? state.User.FindFirst(ClaimTypes.Email)?.Value
              ?? ""
            : "";
    }

    public void Dispose() => AuthState.AuthenticationStateChanged -= OnAuthStateChanged;
}
