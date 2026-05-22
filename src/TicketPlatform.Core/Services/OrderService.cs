using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Core.Services;

public class OrderService(IRepository<Order> repository) : IOrderService
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await repository.Query()
            .Include(o => o.Customer)
            .Include(o => o.Payment)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Tickets)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
                    .ThenInclude(tt => tt.Event)
                        .ThenInclude(e => e.Host)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Order> CreateAsync(Order entity, CancellationToken ct = default)
    {
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<List<PurchaseHistoryItemDTO>> GetPurchaseHistoryAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var orders = await repository.Query()
            .AsNoTracking()
            .Where(o => o.Customer.UserId == userId)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
                    .ThenInclude(tt => tt.Event)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return orders
            .SelectMany(o => o.OrderItems.Select(oi => new PurchaseHistoryItemDTO
            {
                OrderId          = o.Id,
                EventTitle       = oi.TicketType.Event.Title,
                TicketTypeTitle  = oi.TicketType.Title,
                EventDate        = oi.TicketType.OccurenceStartDate,
                Quantity         = oi.Quantity,
                UnitPriceCents   = oi.UnitPriceCents,
                TotalPriceCents  = oi.Quantity * oi.UnitPriceCents,
                Currency         = oi.Currency,
                Status           = o.Status.ToString(),
                PurchasedAt      = o.CreatedAt
            }))
            .ToList();
    }
    public async Task<Order> UpdateAsync(Order entity, CancellationToken ct = default)
    {
        repository.Update(entity);
        await repository.SaveChangesAsync(ct);
        return entity;
    }
}
