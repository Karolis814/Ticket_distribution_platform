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
    IReadOnlyList<TicketTypeDto> TicketTypes
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
