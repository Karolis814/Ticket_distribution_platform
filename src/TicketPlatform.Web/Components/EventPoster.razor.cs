using Microsoft.AspNetCore.Components;

namespace TicketPlatform.Web.Components;

public partial class EventPoster : ComponentBase
{
    [Parameter] public string? Url { get; set; }
    [Parameter] public string Alt { get; set; } = "Event poster";
    [Parameter] public string Style { get; set; } = "";
}
