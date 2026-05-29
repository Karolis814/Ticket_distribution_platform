using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using TicketPlatform.Web.Services;

public class ForgotPasswordBase : ComponentBase
{
    [Inject] protected IForgotPasswordClient ForgotPasswordClient { get; set; } = null!;
    protected ForgotPasswordModel Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected string? Message { get; set; }
    protected bool IsSubmitted { get; set; }

    protected async Task HandleSubmit()
    {
        IsLoading = true;
        Message = null;

        try
        {
            await ForgotPasswordClient.RequestPasswordResetAsync(Model.Email);
        }
        catch { }

        Message = "If an account with that address exists, you'll receive a reset link shortly.";
        IsSubmitted = true;

        IsLoading = false;
    }

    protected class ForgotPasswordModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;
    }
}
