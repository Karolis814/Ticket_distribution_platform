using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public class TicketService
{
    private readonly IRepository<Ticket> _repository;

    public TicketService(IRepository<Ticket> repository)
    {
        _repository = repository;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync(id, ct);
    }

    public async Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct = default)
    {
        return await _repository.ListAsync(ct);
    }

    public async Task<Ticket> CreateAsync(Ticket entity, CancellationToken ct = default)
    {
        if (entity.Price < 0)
        {
            throw new Exception("Ticket price cannot be negative.");
        }

        await _repository.AddAsync(entity, ct);

        await _repository.SaveChangesAsync(ct);

        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);

        if (entity == null)
        {
            throw new Exception("Ticket not found.");
        }

        _repository.Remove(entity);

        await _repository.SaveChangesAsync(ct);
    }
}
