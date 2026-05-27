using System.Net.Http.Json;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public interface IAuthClient
{
    Task<(bool success, string? error)> RegisterAsync(UserSignUpDTO dto);
    Task<(bool success, string? error)> LoginAsync(UserLoginDTO dto);
    Task LogoutAsync();
    Task RefreshAsync();
}