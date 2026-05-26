using System.Net.Http.Json;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public class HostPaymentsClient(HttpClient http) : IHostPaymentsClient
{
    public Task<StripeConnectStatusDto?> GetStatusAsync(Guid hostId, CancellationToken ct = default)
        => http.GetFromJsonAsync<StripeConnectStatusDto>($"api/stripe-connect/status/{hostId}", ct);
}