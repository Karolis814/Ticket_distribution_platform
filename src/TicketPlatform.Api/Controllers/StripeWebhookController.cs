using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Mail.Templates;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/stripe-webhook")]
public class StripeWebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IOrderService _orderService;
    private readonly ITicketService _ticketService;
    private readonly ITicketPdfService _pdfService;
    private readonly IMailService _mailService;
    private readonly IRepository<Payment> _paymentRepository;
    public StripeWebhookController(
        IConfiguration configuration,
        IOrderService orderService,
        ITicketService ticketService,
        ITicketPdfService pdfService,
        IMailService mailService,
        IRepository<Payment> paymentRepository)
    {
        _configuration = configuration;
        _orderService = orderService;
        _ticketService = ticketService;
        _pdfService = pdfService;
        _mailService = mailService;
        _paymentRepository = paymentRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _configuration["Stripe:WebhookSecret"]
            );

            Console.WriteLine($"Stripe event received: {stripeEvent.Type}");

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;

                if (session?.Metadata == null ||
                    !session.Metadata.TryGetValue("orderId", out var orderIdRaw) ||
                    !Guid.TryParse(orderIdRaw, out var orderId))
                {
                    Console.WriteLine("checkout.session.completed without valid orderId.");
                    return Ok();
                }

                var order = await _orderService.GetByIdAsync(orderId, ct);

                if (order is null)
                {
                    Console.WriteLine($"Order {orderId} not found.");
                    return Ok();
                }

                if (order.Status == OrderStatus.Completed)
                {
                    Console.WriteLine($"Order {orderId} already completed.");
                    return Ok();
                }

                order.Status = OrderStatus.Completed;
                order.UpdatedAt = DateTimeOffset.UtcNow;

                var payment = await _paymentRepository.Query()
                    .FirstOrDefaultAsync(p => p.OrderId == order.Id, ct);

                if (payment is not null)
                {
                    payment.StripeCheckoutSessionId = session.Id;
                    payment.StripePaymentIntentId = session.PaymentIntentId;
                    payment.StripeStatus = session.PaymentStatus;
                    payment.SucceededAt = DateTimeOffset.UtcNow;
                    payment.UpdatedAt = DateTimeOffset.UtcNow;

                    _paymentRepository.Update(payment);
                }

                foreach (var orderItem in order.OrderItems)
                {
                    if (orderItem.Tickets.Count >= orderItem.Quantity)
                        continue;

                    var missing = orderItem.Quantity - orderItem.Tickets.Count;

                    for (var i = 0; i < missing; i++)
                    {
                        await _ticketService.CreateAsync(new Ticket
                        {
                            TicketTypeId = orderItem.TicketTypeId,
                            OrderItemId = orderItem.Id,
                            TimesUsed = 0
                        }, ct);
                    }
                }

                await _orderService.UpdateAsync(order, ct);

                var pdf = await _pdfService.GeneratePdfAsync(order.Id, ct);

                var eventName = order.OrderItems
                    .First()
                    .TicketType
                    .Event
                    .Title;

                await _mailService.SendAsync(EmailTemplates.TicketDelivery(
                    toEmail: order.Customer.Email,
                    toName: $"{order.Customer.FirstName} {order.Customer.LastName}",
                    eventTitle: eventName,
                    pdf: pdf), ct);

                Console.WriteLine($"Order {order.Id} completed and tickets emailed.");
            }

            if (stripeEvent.Type == "invoice.paid")
            {
                var invoice = stripeEvent.Data.Object as Invoice;

                if (invoice?.Metadata == null ||
                    !invoice.Metadata.TryGetValue("orderId", out var orderIdRaw) ||
                    !Guid.TryParse(orderIdRaw, out var orderId))
                {
                    Console.WriteLine("invoice.paid without valid orderId.");
                    return Ok();
                }

                var payment = await _paymentRepository.Query()
                    .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

                if (payment is not null)
                {
                    payment.StripeInvoiceId = invoice.Id;
                    payment.StripeInvoiceUrl = invoice.HostedInvoiceUrl;
                    payment.StripeInvoicePdfUrl = invoice.InvoicePdf;
                    payment.UpdatedAt = DateTimeOffset.UtcNow;

                    _paymentRepository.Update(payment);
                    await _paymentRepository.SaveChangesAsync(ct);
                }

                Console.WriteLine($"Invoice paid: {invoice.Id}");
            }

            if (stripeEvent.Type == "invoice.payment_failed")
            {
                var invoice = stripeEvent.Data.Object as Invoice;

                if (invoice?.Metadata != null &&
                    invoice.Metadata.TryGetValue("orderId", out var orderIdRaw) &&
                    Guid.TryParse(orderIdRaw, out var orderId))
                {
                    var order = await _orderService.GetByIdAsync(orderId, ct);

                    if (order is not null && order.Status == OrderStatus.AwaitingPayment)
                    {
                        order.Status = OrderStatus.Canceled;
                        order.UpdatedAt = DateTimeOffset.UtcNow;
                        await _orderService.UpdateAsync(order, ct);
                    }
                }

                Console.WriteLine($"Invoice payment failed: {invoice?.Id}");
            }

            if (stripeEvent.Type == "charge.refunded")
            {
                var charge = stripeEvent.Data.Object as Charge;

                if (!string.IsNullOrWhiteSpace(charge?.PaymentIntentId))
                {
                    var payment = await _paymentRepository.Query()
                        .FirstOrDefaultAsync(p => p.StripePaymentIntentId == charge.PaymentIntentId, ct);

                    if (payment is not null)
                    {
                        var order = await _orderService.GetByIdAsync(payment.OrderId, ct);

                        if (order is not null && order.Status != OrderStatus.Refunded)
                        {
                            order.Status = OrderStatus.Refunded;
                            order.UpdatedAt = DateTimeOffset.UtcNow;
                            await _orderService.UpdateAsync(order, ct);
                        }
                    }
                }

                Console.WriteLine($"Charge refunded: {charge?.Id}");
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"Stripe webhook error: {ex.Message}");
            return BadRequest();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Webhook error: {ex.Message}");
            return BadRequest();
        }
    }
}
