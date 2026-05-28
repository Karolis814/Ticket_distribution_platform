using Microsoft.AspNetCore.Components;

namespace TicketPlatform.Web.Components;

public partial class RedirectToUnauthorized : ComponentBase
{
    [Inject] private NavigationManager Nav { get; set; } = default!;

    protected override void OnInitialized() => Nav.NavigateTo("/unauthorized");
}
