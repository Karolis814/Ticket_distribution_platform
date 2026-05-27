using Microsoft.AspNetCore.Components;

namespace TicketPlatform.Web.Pages;

public class OwnerStripeRefreshBase : ComponentBase
{
    [Inject] private NavigationManager Nav { get; set; } = null!;

    [Parameter] public Guid HostId { get; set; }

    protected void GoToSettings() => Nav.NavigateTo("/user/settings");
}
