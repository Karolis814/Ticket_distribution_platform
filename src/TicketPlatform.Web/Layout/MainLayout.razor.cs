using Microsoft.AspNetCore.Components;
using TicketPlatform.Shared.Dtos;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace TicketPlatform.Web.Layout;

public class MainLayoutBase : LayoutComponentBase
{
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected HttpClient Http { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;
    protected string user {get; set;} = "";
    protected bool SidebarExpanded = true;

    protected void ToggleSidebar()
    {
        SidebarExpanded = !SidebarExpanded;
    }
    protected void NavigateToLogin() => Navigation.NavigateTo("login");
    protected void NavigateToRegister() => Navigation.NavigateTo("register");


    protected override async Task OnParametersSetAsync()
    {
        var state = await AuthState;
        if (state.User.Identity?.IsAuthenticated ?? false)
        {
            //ser = state.User.Identity.Name ?? "Unknown User"; 
            
            
             user = (await Http.GetFromJsonAsync<WhoAmIDTO>("api/auth/me")).email;
        }
    }
    protected override async Task OnInitializedAsync()
    {
            var userResult = await Http.GetFromJsonAsync<WhoAmIDTO>("api/auth/me");
            if (userResult is not null)
                user = userResult.email;
    }
    protected void HandleLogout()
    {
        Navigation.NavigateTo("logout");
    }
}
