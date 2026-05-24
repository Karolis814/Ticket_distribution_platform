namespace TicketPlatform.Web.Services;

public interface IUsersClient
{
    Task<Guid> GetCurrentUserIdAsync(CancellationToken ct = default);
}