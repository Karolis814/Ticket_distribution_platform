using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public interface IHostPaymentsClient
{
    Task<StripeConnectStatusDto?> GetStatusAsync(Guid hostId, CancellationToken ct = default);
}
