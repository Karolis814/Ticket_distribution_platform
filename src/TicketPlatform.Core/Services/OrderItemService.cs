using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public class OrderItemService(IRepository<OrderItem> repository) : IOrderItemService
{
    public async Task<OrderItem> CreateAsync(OrderItem orderItem, CancellationToken ct = default)
    {
        await repository.AddAsync(orderItem, ct);
        await repository.SaveChangesAsync(ct);
        return orderItem;
    }
}
