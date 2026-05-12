namespace TicketPlatform.Shared.Dtos;

public record TicketTypeDto(
    Guid Id,
    Guid EventId,
    string Title,
    DateTimeOffset OccurenceStartDate,
    DateTimeOffset OccurenceEndDate,
    DateTimeOffset AdmissionStartDate,
    DateTimeOffset AdmissionEndDate,
    int PriceCents,
    string Currency,
    int MaxUses,
    int Quantity,
    int Sold
);

public record CreateTicketTypeRequest(
    Guid EventId,
    string Title,
    DateTimeOffset OccurenceStartDate,
    DateTimeOffset OccurenceEndDate,
    DateTimeOffset AdmissionStartDate,
    DateTimeOffset AdmissionEndDate,
    int PriceCents,
    string Currency,
    int MaxUses,
    int Quantity
);
