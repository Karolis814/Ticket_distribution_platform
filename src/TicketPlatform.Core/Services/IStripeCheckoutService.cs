using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface IStripeCheckoutService
{
    Task<string> CreateCheckoutSessionAsync(
        Order order,
        CancellationToken ct = default);
}
