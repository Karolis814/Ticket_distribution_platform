using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketPlatform.Web.Services;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Pages;

public partial class LoginBase : ComponentBase
{
    [Inject] private IAuthClient AuthClient { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;

    [SupplyParameterFromQuery] private string? ReturnUrl { get; set; }

    protected readonly UserLoginDTO Model = new() { Email = "", Password = "" };
    protected string? ErrorMessage;
    protected bool IsLoading;

    protected async Task HandleLogin()
    {
        IsLoading = true;
        ErrorMessage = null;

        var (success, error) = await AuthClient.LoginAsync(Model);

        if (success)
        {
            if (!string.IsNullOrWhiteSpace(ReturnUrl))
            {
                Nav.NavigateTo(ReturnUrl);
            }
            else
            {
                var auth = await AuthState.GetAuthenticationStateAsync();
                var destination = auth.User.IsInRole("Host")
                    ? "/host/events"
                    : "/";
                Nav.NavigateTo(destination);
            }
        }
        else
            ErrorMessage = error;

        IsLoading = false;
    }
}
