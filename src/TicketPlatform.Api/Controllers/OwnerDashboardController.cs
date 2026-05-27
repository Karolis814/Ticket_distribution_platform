using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/owner-dashboard")]
public class OwnerDashboardController(
    IRepository<Order> orderRepository,
    IRepository<Ticket> ticketRepository,
    IHostPaymentSettingsService hostPaymentSettingsService) : ControllerBase
{
    private const decimal PlatformFeeRate = 0.05m;

    [HttpGet("{hostId:guid}")]
    public async Task<ActionResult<OwnerDashboardDto>> GetDashboard(
        Guid hostId,
        CancellationToken ct)
    {
        var completedOrders = await orderRepository.Query()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
                    .ThenInclude(tt => tt.Event)
            .Where(o => o.Status == OrderStatus.Completed)
            .Where(o => o.OrderItems.Any(
                oi => oi.TicketType.Event.HostId == hostId))
            .ToListAsync(ct);

        var hostOrderItems = completedOrders
            .SelectMany(o => o.OrderItems)
            .Where(oi => oi.TicketType.Event.HostId == hostId)
            .ToList();

        var grossRevenue = hostOrderItems
            .GroupBy(oi => oi.Currency.ToLower())
            .Select(g => new MoneyAmountDto(
                g.Sum(x => (long)x.UnitPriceCents * x.Quantity),
                g.Key))
            .ToList();

        var platformFees = grossRevenue
            .Select(x => new MoneyAmountDto(
                (long)Math.Round(x.AmountCents * PlatformFeeRate),
                x.Currency))
            .ToList();

        var netRevenue = grossRevenue
            .Select(x =>
            {
                var fee = (long)Math.Round(x.AmountCents * PlatformFeeRate);

                return new MoneyAmountDto(
                    x.AmountCents - fee,
                    x.Currency);
            })
            .ToList();

        var soldTickets = hostOrderItems.Sum(x => x.Quantity);

        var tickets = await ticketRepository.Query()
            .Include(t => t.TicketType)
                .ThenInclude(tt => tt.Event)
            .Where(t => t.TicketType.Event.HostId == hostId)
            .ToListAsync(ct);

        var usedTickets = tickets.Count(t => t.TimesUsed > 0);
        var validTickets = tickets.Count(t => t.TimesUsed == 0);
        var invalidTickets = tickets.Count(t => t.TimesUsed > t.TicketType.MaxUses);

        var revenueByEvent = hostOrderItems
            .GroupBy(oi => new
            {
                oi.TicketType.Event.Title,
                oi.Currency
            })
            .Select(g => new EventRevenueDto(
                EventName: g.Key.Title,
                TicketsSold: g.Sum(x => x.Quantity),
                RevenueCents: g.Sum(x => x.UnitPriceCents * x.Quantity),
                Currency: g.Key.Currency
            ))
            .OrderByDescending(x => x.RevenueCents)
            .ToList();

        var dailySales = completedOrders
            .GroupBy(o => new
            {
                Date = DateOnly.FromDateTime(o.CreatedAt.UtcDateTime),
                o.Currency
            })
            .Select(g => new DailySalesDto(
                Date: g.Key.Date,
                TicketsSold: g.Sum(x => x.OrderItems.Sum(oi => oi.Quantity)),
                RevenueCents: g.Sum(x => x.TotalPriceCents),
                Currency: g.Key.Currency
            ))
            .OrderBy(x => x.Date)
            .ToList();

        var settings = await hostPaymentSettingsService.GetByHostIdAsync(hostId, ct);

        var availableBalance = new List<MoneyAmountDto>();
        var pendingBalance = new List<MoneyAmountDto>();
        var payouts = new List<StripePayoutDto>();

        if (settings is not null &&
            !string.IsNullOrWhiteSpace(settings.StripeAccountId))
        {
            var requestOptions = new RequestOptions
            {
                StripeAccount = settings.StripeAccountId
            };

            var balanceService = new BalanceService();

            var balance = await balanceService.GetAsync(
                requestOptions: requestOptions,
                cancellationToken: ct);

            availableBalance = balance.Available
                .Select(x => new MoneyAmountDto(x.Amount, x.Currency))
                .ToList();

            pendingBalance = balance.Pending
                .Select(x => new MoneyAmountDto(x.Amount, x.Currency))
                .ToList();

            var payoutService = new PayoutService();

            var payoutList = await payoutService.ListAsync(
                new PayoutListOptions
                {
                    Limit = 10
                },
                requestOptions,
                ct);

            payouts = payoutList.Data
                .Select(p => new StripePayoutDto(
                    p.Id,
                    p.Amount,
                    p.Currency,
                    p.Status,
                    p.Created))
                .ToList();
        }

        return Ok(new OwnerDashboardDto(
            HostId: hostId,

            GrossRevenue: grossRevenue,
            PlatformFees: platformFees,
            NetRevenue: netRevenue,

            SoldTickets: soldTickets,
            UsedTickets: usedTickets,
            ValidTickets: validTickets,
            InvalidTickets: invalidTickets,

            StripeAvailableBalance: availableBalance,
            StripePendingBalance: pendingBalance,

            RevenueByEvent: revenueByEvent,
            DailySales: dailySales,

            Payouts: payouts
        ));
    }
}
