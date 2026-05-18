using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public class HostPaymentSettingsService(
    IRepository<HostPaymentSettings> repository) : IHostPaymentSettingsService
{
    public async Task<HostPaymentSettings?> GetByHostIdAsync(
        Guid hostId,
        CancellationToken ct = default)
        => await repository.Query()
            .FirstOrDefaultAsync(x => x.HostId == hostId, ct);

    public async Task<HostPaymentSettings> CreateAsync(
        HostPaymentSettings entity,
        CancellationToken ct = default)
    {
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<HostPaymentSettings> UpdateAsync(
        HostPaymentSettings entity,
        CancellationToken ct = default)
    {
        repository.Update(entity);
        await repository.SaveChangesAsync(ct);
        return entity;
    }
}
