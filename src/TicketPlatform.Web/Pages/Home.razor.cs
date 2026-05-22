using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;   

namespace TicketPlatform.Web.Pages;

public class HomeBase : ComponentBase
{
    [Inject] protected NavigationManager Nav { get; set; } = null!;
}
