using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

namespace TicketPlatform.Web.Layout;

public class NavMenuBase : ComponentBase, IDisposable
{
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private bool _isAuthenticated;
    protected bool IsHost;

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
        _isAuthenticated = state.User.Identity?.IsAuthenticated ?? false;
        IsHost = _isAuthenticated && state.User.IsInRole("Host");
    }

    public void Dispose() => AuthState.AuthenticationStateChanged -= OnAuthStateChanged;
}
