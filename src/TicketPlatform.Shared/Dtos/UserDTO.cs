using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Shared.Dtos;


public record UserDTO(
    string Name,
    string Email,
    string Role,
    string Company,
    string Address,
    string TaxCode,
    string PhoneNumber
);