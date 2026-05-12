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
    ITicketService ticketService,
    IOrderItemService orderItemService,
    IOrderService orderService,
    ITicketTypeService ticketTypeService,
    ITicketPdfService pdfService,
    IMailService mail) : ControllerBase
{
    [HttpGet("{orderId:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid orderId, CancellationToken ct)
    {
        var pdf = await pdfService.GeneratePdfAsync(orderId, ct);
        return File(pdf, "application/pdf", $"tickets.pdf");
    }

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
                return Conflict(
                    $"Only {remaining} ticket(s) remaining for '{ticketType.Title}'.");

            if (ticketType.OccurenceEndDate < DateTimeOffset.UtcNow)
                return Conflict($"Event '{ticketType.Event.Title}' has ended.");

            ticketTypes.Add((ticketType, item.Quantity));
        }

        var currencies = ticketTypes.Select(x => x.TicketType.Currency).Distinct().ToList();
        if (currencies.Count > 1)
            return BadRequest(
                $"All items must share the same currency. Found: {string.Join(", ", currencies)}.");

        var currency = currencies[0];

        // TODO: Check if a registered user with that email exists.
        //       Use an existing customer entry if so.
        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email
        };
        await customerService.CreateAsync(customer, ct);

        var totalCents = ticketTypes.Sum(x => x.TicketType.PriceCents * x.Quantity);
        var order = new Order
        {
            CustomerId = customer.Id,
            TotalPriceCents = totalCents,
            Currency = currency,
            Status = OrderStatus.Pending
        };
        await orderService.CreateAsync(order, ct);

        var generatedTickets = new List<Ticket>();

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

            for (var i = 0; i < quantity; i++)
            {
                var ticket = await ticketService.CreateAsync(new Ticket
                {
                    TicketTypeId = ticketType.Id,
                    OrderItemId = orderItem.Id,
                }, ct);
                generatedTickets.Add(ticket);
            }
        }

        order.Status = OrderStatus.Completed;
        await orderService.UpdateAsync(order, ct);

        var pdf = await pdfService.GeneratePdfAsync(order.Id, ct);


        await mail.SendTicketAsync(
            customer.Email,
            $"{customer.FirstName} {customer.LastName}",
            ticketTypes.First().TicketType.Event.Title,
            pdf,
            ct);

        var downloadUrl = Url.Action(
            nameof(DownloadPdf),
            "Tickets",
            new { orderId = order.Id },
            Request.Scheme)!;

        return Ok(new CheckoutResponseDto(
            order.Id,
            generatedTickets.Select(t => t.Id).ToList(),
            downloadUrl,
            customer.Email));
    }

    private static TicketDto MapToDto(Ticket t) => new(
        t.Id,
        t.TicketTypeId,
        t.OrderItemId,
        t.TimesUsed);
}
