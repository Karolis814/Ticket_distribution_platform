namespace TicketPlatform.Shared.Dtos;

public record StripeConnectStatusDto(
    Guid HostId,
    string? StripeAccountId,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted,
    bool Ready
);
