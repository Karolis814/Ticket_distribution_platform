using Microsoft.AspNetCore.Components;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public class LogoutBase : ComponentBase
{
    [Inject] private IAuthClient AuthClient { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await AuthClient.LogoutAsync();
        Navigation.NavigateTo("/", forceLoad: true);
    }
}
