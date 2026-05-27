using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(
    IRepository<Payment> paymentRepository,
    ITicketPdfService ticketPdfService) : ControllerBase
{
    [HttpGet("success")]
    public async Task<ActionResult<PaymentSuccessDto>> GetPaymentSuccess(
        [FromQuery] string sessionId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest("Session id is required.");

        var payment = await paymentRepository.Query()
            .Include(p => p.Order)
            .ThenInclude(o => o.Customer)
            .FirstOrDefaultAsync(
                p => p.StripeCheckoutSessionId == sessionId,
                ct);

        if (payment is null)
            return NotFound("Payment was not found yet. Try again in a moment.");

        return Ok(new PaymentSuccessDto(
            payment.OrderId,
            payment.Order.Customer.Email,
            payment.StripeInvoiceUrl,
            payment.StripeInvoicePdfUrl,
            !string.IsNullOrWhiteSpace(payment.StripeInvoiceUrl)
        ));
    }

    [HttpGet("free-success")]
    public async Task<ActionResult<PaymentSuccessDto>> GetFreePaymentSuccess(
        [FromQuery] Guid orderId,
        CancellationToken ct)
    {
        var payment = await paymentRepository.Query()
            .Include(p => p.Order)
            .ThenInclude(o => o.Customer)
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

        if (payment is null || payment.Order.TotalPriceCents != 0)
            return NotFound("Free order not found.");

        return Ok(new PaymentSuccessDto(
            payment.OrderId,
            payment.Order.Customer.Email,
            null,
            null,
            false));
    }

    [HttpGet("{orderId:guid}/tickets")]
    public async Task<IActionResult> DownloadTicketsByOrder(
        Guid orderId,
        [FromQuery] string? sessionId,
        CancellationToken ct)
    {
        var payment = await paymentRepository.Query()
            .Include(p => p.Order)
                .ThenInclude(o => o.Customer)
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

        if (payment is null)
            return NotFound();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (Guid.TryParse(userIdStr, out var userId))
        {
            if (payment.Order.Customer.UserId != userId)
                return NotFound();
        }
        else if (!string.IsNullOrWhiteSpace(payment.StripeCheckoutSessionId) &&
                 payment.StripeCheckoutSessionId == sessionId)
        {
            // verified via Stripe session
        }
        else if (string.IsNullOrWhiteSpace(payment.StripeCheckoutSessionId) &&
                 payment.Order.TotalPriceCents == 0)
        {
            // free order — orderId in path is sufficient (GUID is unguessable)
        }
        else
        {
            return Unauthorized();
        }

        if (payment.Order.Status != OrderStatus.Completed)
            return BadRequest("Order is not completed.");

        var pdf = await ticketPdfService.GeneratePdfAsync(orderId, ct);

        return File(pdf, "application/pdf", "tickets.pdf");
    }
}
