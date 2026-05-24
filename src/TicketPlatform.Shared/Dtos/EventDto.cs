using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Shared.Dtos;

public record EventDto(
    Guid Id,
    string Category,
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
    string? Username,
    string Email,
    string? Company
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
