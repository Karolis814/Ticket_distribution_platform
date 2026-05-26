using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TicketPlatform.Web.Services;
using TicketPlatform.Shared.Dtos;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace TicketPlatform.Web.Pages;

public class RegisterBase : ComponentBase
{
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] protected ILocalStorageService LocalStorage { get; set; } = null!;
    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] protected IAuthClient AuthClient { get; set; } = null!;

    protected UserSignUpDTO? Model;

    [Required(ErrorMessage = "First name is required.")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required.")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = "";

    public string ConfirmPassword { get; set; } = "";
    public string Company { get; set; } = "";
    public string Address { get; set; } = "";
    public string TaxCode { get; set; } = "";
    public string PhoneNumber { get; set; } = "";

    protected EditContext EditContext = default!;
    protected string? ErrorMessage;
    protected bool IsLoading;

    protected override Task OnInitializedAsync()
    {
        EditContext = new EditContext(this);
        return Task.CompletedTask;
    }

    protected async Task HandleRegister()
    {
        ErrorMessage = null;

        if (!EditContext.Validate())
            return;

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        IsLoading = true;

        var dto = new UserSignUpDTO(
            FirstName,
            LastName,
            Email,
            Password,
            Company,
            Address,
            TaxCode,
            PhoneNumber
        );

        var (success, error) = await AuthClient.RegisterAsync(dto);


        if (success)
            Nav.NavigateTo("/events");
        else
            ErrorMessage = error;

        IsLoading = false;
    }
}
