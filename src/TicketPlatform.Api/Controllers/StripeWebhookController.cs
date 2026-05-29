using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/stripe-webhook")]
public class StripeWebhookController(
    IConfiguration configuration,
    IOrderService orderService,
    IOrderCompletionService orderCompletionService,
    IRepository<Payment> paymentRepository,
    IRepository<User> userRepository)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                configuration["Stripe:WebhookSecret"]
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

                var order = await orderService.GetByIdAsync(orderId, ct);

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

                var payment = await paymentRepository.Query()
                    .FirstOrDefaultAsync(p => p.OrderId == order.Id, ct);

                if (payment is not null)
                {
                    payment.StripeCheckoutSessionId = session.Id;
                    payment.StripePaymentIntentId = session.PaymentIntentId;
                    payment.UpdatedAt = DateTimeOffset.UtcNow;

                    paymentRepository.Update(payment);
                    await paymentRepository.SaveChangesAsync(ct);
                }

                await orderCompletionService.CompleteAsync(order, ct);

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

                var payment = await paymentRepository.Query()
                    .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

                if (payment is not null)
                {
                    payment.StripeInvoiceId = invoice.Id;
                    payment.StripeInvoiceUrl = invoice.HostedInvoiceUrl;
                    payment.StripeInvoicePdfUrl = invoice.InvoicePdf;
                    payment.UpdatedAt = DateTimeOffset.UtcNow;

                    paymentRepository.Update(payment);
                    await paymentRepository.SaveChangesAsync(ct);
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
                    var order = await orderService.GetByIdAsync(orderId, ct);

                    if (order is not null && order.Status == OrderStatus.AwaitingPayment)
                    {
                        order.Status = OrderStatus.Canceled;
                        order.UpdatedAt = DateTimeOffset.UtcNow;
                        await orderService.UpdateAsync(order, ct);
                    }
                }

                Console.WriteLine($"Invoice payment failed: {invoice?.Id}");
            }

            if (stripeEvent.Type == "account.updated")
            {
                var account = stripeEvent.Data.Object as Account;

                if (account is not null && account.ChargesEnabled && account.PayoutsEnabled)
                {
                    var host = await userRepository.Query()
                        .FirstOrDefaultAsync(u => u.StripeAccountId == account.Id, ct);

                    if (host is not null && host.StripeOnboardedAt is null)
                    {
                        host.Role = UserRole.Host;
                        host.StripeOnboardedAt = DateTimeOffset.UtcNow;
                        host.UpdatedAt = DateTimeOffset.UtcNow;
                        userRepository.Update(host);
                        await userRepository.SaveChangesAsync(ct);
                        Console.WriteLine($"Host {host.Id} onboarding completed via webhook.");
                    }
                }
            }

            if (stripeEvent.Type == "charge.refunded")
            {
                var charge = stripeEvent.Data.Object as Charge;

                if (!string.IsNullOrWhiteSpace(charge?.PaymentIntentId))
                {
                    var payment = await paymentRepository.Query()
                        .FirstOrDefaultAsync(p => p.StripePaymentIntentId == charge.PaymentIntentId, ct);

                    if (payment is not null)
                    {
                        var order = await orderService.GetByIdAsync(payment.OrderId, ct);

                        if (order is not null && order.Status != OrderStatus.Refunded)
                        {
                            order.Status = OrderStatus.Refunded;
                            order.UpdatedAt = DateTimeOffset.UtcNow;
                            await orderService.UpdateAsync(order, ct);
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
