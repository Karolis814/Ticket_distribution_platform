using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface ITicketPdfService
{
    Task<byte[]> GeneratePdfAsync(Guid orderId, CancellationToken ct = default);
}
