using System.ComponentModel.DataAnnotations;

namespace TicketPlatform.Shared.Dtos;

public class UserSignUpDTO
{
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

    public string Company { get; set; } = "";
    public string Address { get; set; } = "";
    public string TaxCode { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
}
