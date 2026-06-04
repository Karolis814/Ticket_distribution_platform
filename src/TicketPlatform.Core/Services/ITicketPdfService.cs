using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface ITicketPdfService
{
    Task<byte[]> GeneratePdfAsync(Guid orderId, TimeZoneInfo? timeZone = null, CancellationToken ct = default);
}
