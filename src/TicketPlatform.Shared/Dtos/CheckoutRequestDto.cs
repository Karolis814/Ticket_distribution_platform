namespace TicketPlatform.Shared.Dtos;

public record TicketTypeQuantityDto(Guid TicketTypeId, int Quantity);

public record CheckoutRequestDto(
    IReadOnlyList<TicketTypeQuantityDto> Items,
    string Email,
    string FirstName,
    string LastName
);
