namespace TicketPlatform.Shared.Dtos;

public record StripeConnectStatusDto(
    Guid HostId,
    string? StripeAccountId,
    bool Ready
);
