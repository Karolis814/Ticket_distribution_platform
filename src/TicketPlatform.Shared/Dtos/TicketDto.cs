using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Shared.Dtos;

public record TicketDto(
    Guid Id,
    Guid EventId,
    int Price,
    string Currency,
    int? SeatNumber,
    DateTimeOffset AdmissionStart,
    DateTimeOffset AdmissionEnd,
    TicketStatus Status
);

public record CreateTicketRequest(
    Guid EventId,
    int Price,
    string Currency,
    int? SeatNumber,
    DateTimeOffset AdmissionStart,
    DateTimeOffset AdmissionEnd
);
