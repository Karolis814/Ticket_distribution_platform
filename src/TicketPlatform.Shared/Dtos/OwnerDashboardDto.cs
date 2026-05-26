namespace TicketPlatform.Shared.Dtos;

public record OwnerDashboardDto(
    Guid HostId,

    IReadOnlyList<MoneyAmountDto> GrossRevenue,
    IReadOnlyList<MoneyAmountDto> PlatformFees,
    IReadOnlyList<MoneyAmountDto> NetRevenue,

    int SoldTickets,
    int UsedTickets,
    int ValidTickets,
    int InvalidTickets,

    IReadOnlyList<MoneyAmountDto> StripeAvailableBalance,
    IReadOnlyList<MoneyAmountDto> StripePendingBalance,

    IReadOnlyList<EventRevenueDto> RevenueByEvent,
    IReadOnlyList<DailySalesDto> DailySales,

    IReadOnlyList<StripePayoutDto> Payouts
);

public record MoneyAmountDto(
    long AmountCents,
    string Currency
);

public record StripePayoutDto(
    string Id,
    long AmountCents,
    string Currency,
    string Status,
    DateTime Created
);

public record EventRevenueDto(
    string EventName,
    int TicketsSold,
    long RevenueCents,
    string Currency
);

public record DailySalesDto(
    DateOnly Date,
    int TicketsSold,
    long RevenueCents,
    string Currency
);
