
namespace TicketPlatform.Shared.Dtos;
public record UserSignUpDTO(
    string Name,
    string Email,
    string Password,            // authenticate in frontend that password match.
    string role,                            //UserRole Role might be good if want to hard code role for now
    string Company,
    string Address,
    string TaxCode,
    string PhoneNumber
);