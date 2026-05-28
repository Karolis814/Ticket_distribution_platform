using Microsoft.AspNetCore.Components;
using TicketPlatform.Web.Services;
public class ForgotPasswordBase : ComponentBase
{
    
    [Inject] protected IForgotPasswordClient forgotPasswordClient { get; set; } = default!;
    protected ForgotPasswordModel Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected string? Message { get; set; }

    protected async Task HandleSubmit()
    {
        IsLoading = true;
        Message = null;

        try
        {
            
            if (await forgotPasswordClient.RequestPasswordResetAsync(Model.Email))
                Message = "A reset link has been sent.";
            else 
                Message = "If an account exists, a reset link has been sent.";
        }
        catch
        {
            Message = "If an account exists, a reset link has been sent.";
        }

        IsLoading = false;
    }

    protected class ForgotPasswordModel
    {
        public string Email { get; set; } = string.Empty;
    }
}