using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public interface IHostPaymentsClient
{
    Task<HostStripeStatusDto> GetStatusAsync(Guid hostId, CancellationToken ct = default);
}
