using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace TicketPlatform.Web.Services;

public class JwtAuthStateProvider(ILocalStorageService localStorage) : AuthenticationStateProvider
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await localStorage.GetItemAsStringAsync("authToken");

        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var jwt = _handler.ReadJwtToken(token);

        if (jwt.ValidTo < DateTime.UtcNow)
        {
            await localStorage.RemoveItemAsync("authToken");
            return Anonymous;
        }

        var identity = new ClaimsIdentity(jwt.Claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserLoggedIn(string token)
    {
        var jwt = _handler.ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void NotifyUserLoggedOut()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }
}