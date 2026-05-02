namespace TicketPlatform.Shared.Events;

public record EventDto(
    Guid Id,
    string Title,
    string Description,
    string Location,
    DateTime StartsAt,
    int Capacity);

public record CreateEventRequest(
    string Title,
    string Description,
    string Location,
    DateTime StartsAt,
    int Capacity);
