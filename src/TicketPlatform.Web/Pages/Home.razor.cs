using Microsoft.AspNetCore.Components;

namespace TicketPlatform.Web.Pages;

public class HomeBase : ComponentBase
{
    [Inject] protected NavigationManager Nav { get; set; } = null!;
}
