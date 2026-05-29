using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Exceptions;

namespace TicketPlatform.Core.Services;

public class TicketTypeService(IRepository<TicketType> repository) : ITicketTypeService
{
    public async Task<TicketType?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await repository.Query()
            .Include(tt => tt.Event)
            .Include(tt => tt.Tickets)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<TicketType> CreateAsync(TicketType ticketType, CancellationToken ct = default)
    {
        await repository.AddAsync(ticketType, ct);
        await repository.SaveChangesAsync(ct);
        return ticketType;
    }

    public async Task ReserveAsync(Guid ticketTypeId, int quantity, CancellationToken ct = default)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var ticketType = await repository.Query()
                .Include(tt => tt.Tickets)
                .FirstOrDefaultAsync(tt => tt.Id == ticketTypeId, ct);

            if (ticketType is null)
                throw new SoldOutException($"Ticket type {ticketTypeId} not found.");

            var remaining = ticketType.Quantity - ticketType.Tickets.Count;

            if (remaining < quantity)
                throw new SoldOutException(
                    $"Only {remaining} ticket(s) remaining for '{ticketType.Title}'.");

            ticketType.UpdatedAt = DateTimeOffset.UtcNow;
            repository.Update(ticketType);

            try
            {
                await repository.SaveChangesAsync(ct);
                return; // committed successfully
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                // Another request committed first and Version changed.
                // Loop re-reads a fresh row on next iteration.
            }
            catch (DbUpdateConcurrencyException)
            {
                // All retries exhausted
                throw new SoldOutException(
                    $"Could not reserve tickets for '{ticketType.Title}' due to high demand. Please try again.");
            }
        }
    }
}
