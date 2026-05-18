using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface IHostPaymentSettingsService
{
    Task<HostPaymentSettings?> GetByHostIdAsync(Guid hostId, CancellationToken ct = default);
    Task<HostPaymentSettings> CreateAsync(HostPaymentSettings entity, CancellationToken ct = default);
    Task<HostPaymentSettings> UpdateAsync(HostPaymentSettings entity, CancellationToken ct = default);
}
