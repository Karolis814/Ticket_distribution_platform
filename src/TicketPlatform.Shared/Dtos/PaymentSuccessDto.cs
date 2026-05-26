namespace TicketPlatform.Shared.Dtos;

public record PaymentSuccessDto(
    Guid OrderId,
    string Email,
    string? InvoiceUrl,
    string? InvoicePdfUrl,
    bool InvoiceReady
);
