using TicketPlatform.Core.Entities;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Core.Services;


public interface IOrderService
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order> CreateAsync(Order entity, CancellationToken ct = default);
    Task<Order> UpdateAsync(Order entity, CancellationToken ct = default);
    Task<List<PurchaseHistoryItemDTO>> GetPurchaseHistoryAsync(Guid userId, CancellationToken ct = default);
}
