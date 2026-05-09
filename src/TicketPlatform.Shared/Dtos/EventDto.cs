namespace TicketPlatform.Shared.Dtos;

public record EventDto(
    Guid Id,
    string Title,
    string Description,
    string Location,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int TicketCount,
    Guid HostId
);

public record CreateEventRequest(
    string Title,
    string Description,
    string Location,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int TicketCount,
    Guid HostId
);
