using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace TicketPlatform.Web.Layout;

public class MainLayoutBase : LayoutComponentBase
{
    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = null!;
    protected string User { get; set; } = "";
    protected bool SidebarExpanded = true;

    protected void ToggleSidebar() => SidebarExpanded = !SidebarExpanded;
    protected void NavigateToLogin() => Navigation.NavigateTo("/auth/login");
    protected void NavigateToRegister() => Navigation.NavigateTo("/auth/register");

    protected override async Task OnParametersSetAsync()
    {
        var state = await AuthState;
        if (state.User.Identity?.IsAuthenticated ?? false)
        {
            User = state.User.FindFirst("email")?.Value
                ?? state.User.FindFirst(ClaimTypes.Email)?.Value
                ?? "";
        }
        else
        {
            User = "";
        }
    }

    protected void HandleLogout() => Navigation.NavigateTo("/auth/logout");
}
