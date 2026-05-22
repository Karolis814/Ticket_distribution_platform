using Microsoft.AspNetCore.Components;
using TicketPlatform.Web.Services;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Pages;

public partial class LoginBase : ComponentBase
{
    [Inject] private IAuthClient AuthClient { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    protected readonly UserLoginDTO? model = new UserLoginDTO();
    protected string? errorMessage;
    protected bool isLoading;

    protected async Task HandleLogin()
    {
        isLoading = true;
        errorMessage = null;

        var (success, error) = await AuthClient.LoginAsync(model);

        if (success)
            Nav.NavigateTo("/");
        else
            errorMessage = error;

        isLoading = false;
    }
}