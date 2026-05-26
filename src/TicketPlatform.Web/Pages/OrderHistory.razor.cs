using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using TicketPlatform.Web.Services;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Pages;

public class OrderHistoryBase : ComponentBase, IAsyncDisposable
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
        var response = await Http.GetAsync($"api/payments/{orderId}/tickets");
        if (!response.IsSuccessStatusCode) return;

        var bytes = await response.Content.ReadAsByteArrayAsync();
        _jsModule ??= await Js.InvokeAsync<IJSObjectReference>("import", "./js/downloads.js");
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
