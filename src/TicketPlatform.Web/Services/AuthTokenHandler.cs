using Blazored.LocalStorage;

namespace TicketPlatform.Web.Services;

public class AuthTokenHandler(ILocalStorageService localStorage) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        var token = await localStorage.GetItemAsStringAsync("authToken");

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new("Bearer", token);

        return await base.SendAsync(request, ct);
    }
}