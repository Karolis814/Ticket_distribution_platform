using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(
    IRepository<Payment> paymentRepository) : ControllerBase
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
}
