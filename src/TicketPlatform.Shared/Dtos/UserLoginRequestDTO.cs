using System.ComponentModel.DataAnnotations;

namespace TicketPlatform.Shared.Dtos;

public class UserLoginDTO
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    public required string Password { get; set; }
}
