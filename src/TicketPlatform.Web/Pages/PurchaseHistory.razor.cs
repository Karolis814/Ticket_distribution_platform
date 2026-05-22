using Microsoft.AspNetCore.Components;
using TicketPlatform.Web.Services;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Pages;

public class PurchaseHistoryBase : ComponentBase
{
    [Inject] protected IOrdersClient OrdersClient { get; set; } = default!;

    protected List<PurchaseHistoryItemDTO> orders = [];
    protected bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        orders = await OrdersClient.GetPurchaseHistoryAsync();
        isLoading = false;
    }

    protected static string FormatPrice(int cents, string currency) =>
        $"{cents / 100.0m:F2} {currency.ToUpper()}";
}