using System.Net.Http.Json;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public class HostPaymentsClient(HttpClient http) : IHostPaymentsClient
{
    public async Task<HostStripeStatusDto> GetStatusAsync(Guid hostId, CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<StripeConnectStatusDto>(
            $"api/stripe-connect/status/{hostId}",
            ct);

        return result is null
            ? new HostStripeStatusDto(false, false, null)
            : new HostStripeStatusDto(
                Connected: !string.IsNullOrWhiteSpace(result.StripeAccountId),
                Ready: result.Ready,
                StripeAccountId: result.StripeAccountId
            );
    }
}
