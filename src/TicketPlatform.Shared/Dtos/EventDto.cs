using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Shared.Dtos;

public record EventDto(
    Guid Id,
    Guid HostId,
    Guid CategoryId,
    string? CategoryName,
    string Title,
    string Description,
    string? Location,
    string? ThumbnailUrl,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    EventStatus Status,
    IReadOnlyList<TicketTypeDto> TicketTypes
);

public record CreateEventRequest(
    Guid HostId,
    Guid CategoryId,
    string Title,
    string Description,
    string? Location,
    string? ThumbnailUrl,
    EventStatus Status,
    IReadOnlyList<CreateTicketTypeRequest> TicketTypes
);
