using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface IOrderCompletionService
{
    Task CompleteAsync(Order order, CancellationToken ct = default);
}
