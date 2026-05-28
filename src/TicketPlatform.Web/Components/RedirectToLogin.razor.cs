using Microsoft.AspNetCore.Components;

namespace TicketPlatform.Web.Components;

public partial class RedirectToLogin : ComponentBase
{
    [Inject] private NavigationManager Nav { get; set; } = default!;

    protected override void OnInitialized()
    {
        var returnUrl = Uri.EscapeDataString(Nav.ToBaseRelativePath(Nav.Uri));
        Nav.NavigateTo($"/auth/login?returnUrl={returnUrl}");
    }
}
