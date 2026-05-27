using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public interface IOrdersClient
{
    Task<List<PurchaseHistoryItemDTO>> GetPurchaseHistoryAsync();
}