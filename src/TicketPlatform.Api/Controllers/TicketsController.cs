using Microsoft.AspNetCore.Mvc;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController(
    ICustomerService customerService,
    IOrderItemService orderItemService,
    IOrderService orderService,
    ITicketTypeService ticketTypeService,
    IStripeCheckoutService stripeCheckoutService,
    IRepository<Payment> paymentRepository) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResponseDto>> Checkout(
        [FromBody] CheckoutRequestDto request,
        CancellationToken ct)
    {
        if (request.Items is not { Count: > 0 })
            return BadRequest("At least one item is required.");

        if (request.Items.Any(i => i.Quantity < 1))
            return BadRequest("Each item quantity must be at least 1.");

        var ticketTypes = new List<(TicketType TicketType, int Quantity)>();

        foreach (var item in request.Items)
        {
            var ticketType = await ticketTypeService.GetByIdAsync(item.TicketTypeId, ct);

            if (ticketType is null)
                return NotFound($"TicketType {item.TicketTypeId} not found.");

            if (ticketType.Event.Status == EventStatus.Cancelled)
                return Conflict($"Event '{ticketType.Event.Title}' has been cancelled.");

            var remaining = ticketType.Quantity - ticketType.Tickets.Count;

            if (remaining < item.Quantity)
                return Conflict($"Only {remaining} ticket(s) remaining for '{ticketType.Title}'.");

            if (ticketType.OccurenceEndDate < DateTimeOffset.UtcNow)
                return Conflict($"Event '{ticketType.Event.Title}' has ended.");

            ticketTypes.Add((ticketType, item.Quantity));
        }

        var currencies = ticketTypes
            .Select(x => x.TicketType.Currency)
            .Distinct()
            .ToList();

        if (currencies.Count > 1)
            return BadRequest($"All items must share the same currency. Found: {string.Join(", ", currencies)}.");

        var currency = currencies[0];

        var customer = new Customer
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            EmailRemindersEnabled = true
        };

        await customerService.CreateAsync(customer, ct);

        var totalCents = ticketTypes.Sum(x => x.TicketType.PriceCents * x.Quantity);

        var order = new Order
        {
            CustomerId = customer.Id,
            TotalPriceCents = totalCents,
            Currency = currency,
            Status = OrderStatus.AwaitingPayment
        };

        await orderService.CreateAsync(order, ct);

        foreach (var (ticketType, quantity) in ticketTypes)
        {
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                TicketTypeId = ticketType.Id,
                Quantity = quantity,
                UnitPriceCents = ticketType.PriceCents,
                Currency = ticketType.Currency
            };

            await orderItemService.CreateAsync(orderItem, ct);
        }

        var payment = new Payment
        {
            OrderId = order.Id,
            AmountCents = totalCents,
            Currency = currency,
            StripeStatus = "created"
        };

        await paymentRepository.AddAsync(payment, ct);
        await paymentRepository.SaveChangesAsync(ct);

        var loadedOrder = await orderService.GetByIdAsync(order.Id, ct);

        if (loadedOrder is null)
            return Problem("Order was created but could not be loaded.");

        var checkoutUrl = await stripeCheckoutService.CreateCheckoutSessionAsync(loadedOrder, ct);

        return Ok(new CheckoutResponseDto(
            loadedOrder.Id,
            checkoutUrl));
    }
}
