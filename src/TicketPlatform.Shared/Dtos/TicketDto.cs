using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Shared.Dtos;

public record TicketDto(
    Guid Id,
    Guid TicketTypeId,
    Guid OrderItemId,
    int TimesUsed
);
