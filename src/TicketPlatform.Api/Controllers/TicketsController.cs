using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Exceptions;
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
    IOrderCompletionService orderCompletionService,
    IRepository<Payment> paymentRepository,
    IUserService userService,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResponseDto>> Checkout(
        [FromBody] CheckoutRequestDto request,
        CancellationToken ct)
    {

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");
        Guid? userId = Guid.TryParse(userIdStr, out var parsed) ? parsed : null;

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

        try
        {
            foreach (var (ticketType, quantity) in ticketTypes)
                await ticketTypeService.ReserveAsync(ticketType.Id, quantity, ct);
        }
        catch (SoldOutException ex)
        {
            return Conflict(ex.Message);
        }

        string firstName, lastName, email;

        if (userId is not null)
        {
            var user = await userService.GetByIdAsync(userId.Value, ct);
            if (user is null)
                return Unauthorized();

            email     = user.Email;
            firstName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : request.FirstName.Trim();
            lastName  = !string.IsNullOrWhiteSpace(user.LastName)  ? user.LastName  : request.LastName.Trim();
        }
        else
        {
            firstName = request.FirstName.Trim();
            lastName  = request.LastName.Trim();
            email     = request.Email.Trim();
        }

        var customer = new Customer
        {
            FirstName = firstName,
            LastName  = lastName,
            Email     = email,
            UserId    = userId
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

        if (totalCents == 0)
        {
            await orderCompletionService.CompleteAsync(loadedOrder, ct);

            var successUrl = $"{configuration["ClientBaseUrl"]}/checkout/success?order_id={order.Id}";
            return Ok(new CheckoutResponseDto(loadedOrder.Id, successUrl));
        }

        var checkoutUrl = await stripeCheckoutService.CreateCheckoutSessionAsync(loadedOrder, ct);

        return Ok(new CheckoutResponseDto(
            loadedOrder.Id,
            checkoutUrl));
    }
}
