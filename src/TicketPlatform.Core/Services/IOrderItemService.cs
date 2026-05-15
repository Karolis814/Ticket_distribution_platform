using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface IOrderItemService
{
    Task<OrderItem> CreateAsync(OrderItem entity, CancellationToken ct = default);
}
