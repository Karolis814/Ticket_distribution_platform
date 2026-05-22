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

    [Inject] protected IAuthClient AuthClient { get; set; } = default!;

    // Initialize with empty strings to match the required constructor parameters
    protected UserSignUpDTO? model ;

    protected string Name { get; set; } = "";
    protected string Email { get; set; } = "";
    protected string Password { get; set; } = "";
    protected string ConfirmPassword { get; set; } = "";
    protected string SelectedRole { get; set; } = "Customer"; // Default role
    protected string Company { get; set; } = "";
    protected string Address { get; set; } = "";
    protected string TaxCode { get; set; } = "";
    protected string PhoneNumber { get; set; } = "";

     protected EditContext editContext = default!;


    protected string? errorMessage;
    protected bool isLoading;

    protected override async Task OnInitializedAsync()
    {
       // AuthClient = new AuthClient(new HttpClient(), LocalStorage, AuthenticationStateProvider); 
        editContext = new EditContext(this);

    }

    protected async Task HandleRegister()
    {
        isLoading = true;
        errorMessage = null;

        var dto = new UserSignUpDTO(
            Name,
            Email,
            Password,
            ConfirmPassword,
            Company,
            Address,
            TaxCode,
            PhoneNumber
        ) { role = SelectedRole };

       var (success, error) = await AuthClient.RegisterAsync(dto);
         
        if (success)
            Nav.NavigateTo("/login");
        else
            errorMessage = error;

        isLoading = false;
    }
}