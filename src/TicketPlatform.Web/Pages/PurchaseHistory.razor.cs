using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using TicketPlatform.Web.Services;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Pages;

public class PurchaseHistoryBase : ComponentBase, IAsyncDisposable
{
    [Inject] protected HttpClient Http { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] protected IJSRuntime Js { get; set; } = null!;
    [Inject] protected IOrdersClient OrdersClient { get; set; } = null!;

    protected List<PurchaseHistoryItemDTO> Orders = [];
    protected bool IsLoading = true;

    private IJSObjectReference? _jsModule;

    protected override async Task OnInitializedAsync()
    {
        Orders = await OrdersClient.GetPurchaseHistoryAsync();
        IsLoading = false;
    }

    protected async Task DownloadTicketsAsync(Guid orderId)
    {
        _jsModule ??= await Js.InvokeAsync<IJSObjectReference>("import", "./js/downloads.js");
        var tz = await _jsModule.InvokeAsync<string>("getTimezone");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/payments/{orderId}/tickets");
        request.Headers.TryAddWithoutValidation("X-Timezone", tz);
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return;

        var bytes = await response.Content.ReadAsByteArrayAsync();
        await _jsModule.InvokeVoidAsync("triggerDownload", "tickets.pdf", "application/pdf", bytes);
    }

    protected static string FormatPrice(int cents, string currency) =>
        $"{cents / 100.0m:F2} {currency.ToUpper()}";

    protected static BadgeStyle StatusBadge(string status) => status switch
    {
        "Completed" => BadgeStyle.Success,
        "Refunded"  => BadgeStyle.Warning,
        "Canceled"  => BadgeStyle.Danger,
        _           => BadgeStyle.Light
    };

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
            await _jsModule.DisposeAsync();
    }
}
