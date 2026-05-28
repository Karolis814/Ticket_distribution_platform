

namespace TicketPlatform.Web.Services;
public interface IForgotPasswordClient
{
    Task<bool> RequestPasswordResetAsync(string email, CancellationToken ct = default);
}