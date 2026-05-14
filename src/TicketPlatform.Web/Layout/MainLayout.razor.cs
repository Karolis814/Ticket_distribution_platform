using Microsoft.AspNetCore.Components;

namespace TicketPlatform.Web.Layout;

public class MainLayoutBase : LayoutComponentBase
{
    protected bool SidebarExpanded = true;

    protected void ToggleSidebar()
    {
        SidebarExpanded = !SidebarExpanded;
    }
}
