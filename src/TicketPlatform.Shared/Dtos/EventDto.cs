using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Shared.Dtos;

public record EventDto(
    Guid Id,
    Guid HostId,
    string Category,
    string Title,
    string Description,
    string? Location,
    string? ThumbnailUrl,
    EventStatus Status,
    IReadOnlyList<TicketTypeDto> TicketTypes,
    DateTimeOffset CreatedAt
);

public record CreateEventRequest(
    Guid HostId,
    string Category,
    string Title,
    string Description,
    string? Location,
    string? ThumbnailUrl,
    EventStatus Status,
    IReadOnlyList<CreateTicketTypeRequest> TicketTypes
);
