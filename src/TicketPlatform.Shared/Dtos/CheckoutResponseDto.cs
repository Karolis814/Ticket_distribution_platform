namespace TicketPlatform.Shared.Dtos;

public record CheckoutResponseDto(
    Guid OrderId,
    string CheckoutUrl
);
