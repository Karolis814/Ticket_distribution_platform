using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public class JwtAuthStateProvider(
    ILocalStorageService localStorage,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : AuthenticationStateProvider, IDisposable
{
    private const string TokenKey = "authToken";
    private readonly JwtSecurityTokenHandler _handler = new();
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private int RefreshThresholdMinutes => configuration.GetValue("RefreshThresholdMinutes", 5);

    private CancellationTokenSource? _refreshCts;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await localStorage.GetItemAsStringAsync(TokenKey);

        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        JwtSecurityToken jwt;
        try { jwt = _handler.ReadJwtToken(token); }
        catch
        {
            await localStorage.RemoveItemAsync(TokenKey);
            return Anonymous;
        }

        if (jwt.ValidTo < DateTime.UtcNow)
        {
            await localStorage.RemoveItemAsync(TokenKey);
            return Anonymous;
        }

        ScheduleRefresh(jwt.ValidTo);

        var identity = new ClaimsIdentity(jwt.Claims, "jwt", "sub", "role");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserLoggedIn(string token)
    {
        var jwt = _handler.ReadJwtToken(token);
        ScheduleRefresh(jwt.ValidTo, force: true);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt", "sub", "role");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void NotifyUserLoggedOut()
    {
        CancelRefresh();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private void ScheduleRefresh(DateTime validTo, bool force = false)
    {
        // Don't reschedule if one is already running, unless forced (new token after refresh)
        if (!force && _refreshCts is { IsCancellationRequested: false })
            return;

        CancelRefresh();

        var delay = validTo - DateTime.UtcNow - TimeSpan.FromMinutes(RefreshThresholdMinutes);
        if (delay < TimeSpan.Zero)
            delay = TimeSpan.Zero;

        var cts = new CancellationTokenSource();
        _refreshCts = cts;

        _ = Task.Delay(delay, cts.Token).ContinueWith(
            async t =>
            {
                if (t.IsCanceled) return;
                await DoRefreshAsync(cts.Token);
            },
            TaskScheduler.Current);
    }

    private async Task DoRefreshAsync(CancellationToken ct)
    {
        try
        {
            var token = await localStorage.GetItemAsStringAsync(TokenKey);
            if (string.IsNullOrWhiteSpace(token)) return;

            var client = httpClientFactory.CreateClient("AuthorizedClient");
            var response = await client.PostAsync("api/auth/refresh", null, ct);
            if (!response.IsSuccessStatusCode) return;

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDTO>(cancellationToken: ct);
            if (result?.AccessToken is null) return;

            await localStorage.SetItemAsStringAsync(TokenKey, result.AccessToken);
            NotifyUserLoggedIn(result.AccessToken);
        }
        catch (OperationCanceledException) { }
        catch { }
    }

    private void CancelRefresh()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;
    }

    public void Dispose() => CancelRefresh();
}
