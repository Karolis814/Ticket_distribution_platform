namespace TicketPlatform.Shared.Dtos;

public record CheckoutResponseDto(
    Guid OrderId,
    IReadOnlyList<Guid> TicketIds,
    string DownloadUrl,
    string EmailSentTo
);
