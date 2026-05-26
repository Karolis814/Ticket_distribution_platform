namespace TicketPlatform.Shared.Dtos;
public record UserSignUpDTO(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Company,
    string Address,
    string TaxCode,
    string PhoneNumber
);