using System.Net.Http.Json;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public class OrdersClient(HttpClient http) : IOrdersClient
{
    public async Task<List<PurchaseHistoryItemDTO>> GetPurchaseHistoryAsync()
    {
        var result = await http.GetFromJsonAsync<List<PurchaseHistoryItemDTO>>(
            "api/order/purchase-history");
        return result ?? [];
    }
}