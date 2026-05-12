using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public interface ICustomerService
{
    Task<Customer> CreateAsync(Customer entity, CancellationToken ct = default);
}
