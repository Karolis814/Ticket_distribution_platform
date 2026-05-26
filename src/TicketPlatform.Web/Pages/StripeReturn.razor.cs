using Microsoft.AspNetCore.Components;

namespace TicketPlatform.Web.Pages;

public class StripeReturnBase : ComponentBase
{
    [Inject] private NavigationManager Nav { get; set; } = null!;

    [Parameter] public Guid HostId { get; set; }

    protected override void OnInitialized()
        => Nav.NavigateTo("/user/settings", replace: true);
}
