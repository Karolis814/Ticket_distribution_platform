using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Shared.Dtos;

public record EventDto(
    Guid Id,
    Guid HostId,
    EventCategory Category,
    string Title,
    string Description,
    string? Location,
    string? ThumbnailUrl,
    EventStatus Status,
    DateTimeOffset CreatedAt,
    HostDto Host,
    IReadOnlyList<TicketTypeDto> TicketTypes
);

public record HostDto(
    Guid Id,
    string? FirstName,
    string? LastName,
    string Email,
    string? Company,
    string? PhoneNumber,
    string? Address,
    string? TaxCode
);

public record CreateEventRequest(
    Guid HostId,
    EventCategory Category,
    string Title,
    string Description,
    string? Location,
    string? ThumbnailUrl,
    EventStatus Status,
    IReadOnlyList<CreateTicketTypeRequest> TicketTypes
);
