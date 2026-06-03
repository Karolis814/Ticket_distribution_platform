using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Mail.Templates;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Core.Services;

public class OrderCompletionService(
    IOrderService orderService,
    ITicketService ticketService,
    ITicketPdfService pdfService,
    IMailService mailService,
    IRepository<Payment> paymentRepository) : IOrderCompletionService
{
    public async Task CompleteAsync(Order order, CancellationToken ct = default)
    {
        order.Status = OrderStatus.Completed;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var orderItem in order.OrderItems)
        {
            var missing = orderItem.Quantity - orderItem.Tickets.Count;

            for (var i = 0; i < missing; i++)
            {
                await ticketService.CreateAsync(new Ticket
                {
                    TicketTypeId = orderItem.TicketTypeId,
                    OrderItemId = orderItem.Id,
                    TimesUsed = 0
                }, ct);
            }
        }

        await orderService.UpdateAsync(order, ct);

        var payment = await paymentRepository.Query()
            .FirstOrDefaultAsync(p => p.OrderId == order.Id, ct);

        if (payment is not null)
        {
            payment.StripeStatus = "succeeded";
            payment.SucceededAt = DateTimeOffset.UtcNow;
            payment.UpdatedAt = DateTimeOffset.UtcNow;

            paymentRepository.Update(payment);
            await paymentRepository.SaveChangesAsync(ct);
        }

        var pdf = await pdfService.GeneratePdfAsync(order.Id, ct: ct);

        var eventName = order.OrderItems.First().TicketType.Event.Title;

        await mailService.SendAsync(EmailTemplates.TicketDelivery(
            toEmail: order.Customer.Email,
            toName: $"{order.Customer.FirstName} {order.Customer.LastName}",
            eventTitle: eventName,
            pdf: pdf), ct);
    }
}
