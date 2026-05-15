using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface IOrderService
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order> CreateAsync(Order entity, CancellationToken ct = default);
    Task<Order> UpdateAsync(Order entity, CancellationToken ct = default);
}
