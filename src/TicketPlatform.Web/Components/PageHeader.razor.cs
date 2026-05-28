using Microsoft.AspNetCore.Components;

namespace TicketPlatform.Web.Components;

public partial class PageHeader : ComponentBase
{
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public RenderFragment? Actions { get; set; }
}
