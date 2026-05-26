using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public class AuthTokenHandler(ILocalStorageService localStorage, IConfiguration configuration) : DelegatingHandler
{
    private const string TokenKey = "authToken";
    private int RefreshThresholdMinutes => configuration.GetValue("RefreshThresholdMinutes", 2);

    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        var token = await localStorage.GetItemAsStringAsync(TokenKey);

        if (!string.IsNullOrWhiteSpace(token) &&
            !request.RequestUri!.PathAndQuery.Contains("/auth/", StringComparison.OrdinalIgnoreCase))
        {
            token = await TrySilentRefreshAsync(token, ct) ?? token;
        }

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new("Bearer", token);

        return await base.SendAsync(request, ct);
    }

    private async Task<string?> TrySilentRefreshAsync(string token, CancellationToken ct)
    {
        JwtSecurityToken jwt;
        try { jwt = new JwtSecurityTokenHandler().ReadJwtToken(token); }
        catch { return null; }

        var now = DateTime.UtcNow;
        if (jwt.ValidTo <= now || jwt.ValidTo > now.AddMinutes(RefreshThresholdMinutes))
            return null;

        if (!await RefreshLock.WaitAsync(0, ct))
            return null;

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh");
            req.Headers.Authorization = new("Bearer", token);

            var response = await base.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDTO>(cancellationToken: ct);
            if (result?.AccessToken is null)
                return null;

            await localStorage.SetItemAsStringAsync(TokenKey, result.AccessToken);
            return result.AccessToken;
        }
        catch { return null; }
        finally { RefreshLock.Release(); }
    }
}
