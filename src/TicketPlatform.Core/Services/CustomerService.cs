using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public class CustomerService(IRepository<Customer> repository) : ICustomerService
{
    public async Task<Customer> CreateAsync(Customer entity, CancellationToken ct = default)
    {
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return entity;
    }
}
