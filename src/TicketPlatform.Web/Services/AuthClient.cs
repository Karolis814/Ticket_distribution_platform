using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public class AuthClient : IAuthClient
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;

    private const string TokenKey = "authToken";
    public AuthClient(HttpClient http, ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

   

    public async Task<(bool success, string? error)> RegisterAsync(UserSignUpDTO dto)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", dto);

        if (response.IsSuccessStatusCode)
        {
           // var result = await response.Content.ReadFromJsonAsync<AuthResponseDTO>();
         //   await StoreTokenAsync(result!);
            return (true, null);
        }

      //  var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
      //  var message = problem.TryGetProperty("message", out var msg) ? msg.GetString() : "Registration failed.";
        return (false, "FAIL");
    }

    public async Task<(bool success, string? error)> LoginAsync(UserLoginDTO dto)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", dto);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDTO>();
            await StoreTokenAsync(result!);
            return (true, null);
        }

        return (false, "Invalid email or password.");
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        ((JwtAuthStateProvider)_authStateProvider).NotifyUserLoggedOut();
    }

    private async Task StoreTokenAsync(AuthResponseDTO response)
    {
        await _localStorage.SetItemAsStringAsync(TokenKey, response.AccessToken);
        ((JwtAuthStateProvider)_authStateProvider).NotifyUserLoggedIn(response.AccessToken);
    }
}