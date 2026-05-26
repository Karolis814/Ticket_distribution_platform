namespace TicketPlatform.Shared.Dtos;

public record HostStripeStatusDto(
    bool Connected,
    bool Ready,
    string? StripeAccountId
);
