using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;

namespace TicketPlatform.Infrastructure.Payments;

public class StripeCheckoutService(
    IRepository<User> userRepository,
    IConfiguration configuration) : IStripeCheckoutService
{
    private decimal PlatformFeeRate =>
        (configuration.GetValue<decimal?>("Stripe:PlatformFeePercent") ?? 5m) / 100m;

    public async Task<string> CreateCheckoutSessionAsync(
        Order order,
        CancellationToken ct = default)
    {
        if (order.Customer is null)
            throw new InvalidOperationException("Order customer is not loaded.");

        if (order.OrderItems is not { Count: > 0 })
            throw new InvalidOperationException("Order has no order items.");

        var hostIds = order.OrderItems
            .Select(oi => oi.TicketType.Event.HostId)
            .Distinct()
            .ToList();

        if (hostIds.Count != 1)
            throw new InvalidOperationException("All order items must belong to the same event host.");

        var hostId = hostIds[0];

        var host = await userRepository.GetByIdAsync(hostId, ct);

        if (host is null || string.IsNullOrWhiteSpace(host.StripeAccountId))
            throw new InvalidOperationException("Event host has not completed Stripe onboarding.");

        var account = await new AccountService().GetAsync(host.StripeAccountId, cancellationToken: ct);

        if (!account.ChargesEnabled || !account.PayoutsEnabled)
            throw new InvalidOperationException("Event host has not completed Stripe onboarding.");

        var platformFeeCents = (long)Math.Round(order.TotalPriceCents * PlatformFeeRate);

        var metadata = new Dictionary<string, string>
        {
            { "orderId", order.Id.ToString() },
            { "email", order.Customer.Email },
            { "fullName", $"{order.Customer.FirstName} {order.Customer.LastName}" },
            { "hostId", hostId.ToString() },
            { "stripeAccountId", host.StripeAccountId }
        };

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = order.Customer.Email,

            PaymentMethodTypes = new List<string>
            {
                "card"
            },

            LineItems = order.OrderItems
                .GroupBy(oi => oi.TicketTypeId)
                .Select(group =>
                {
                    var first = group.First();

                    return new SessionLineItemOptions
                    {
                        Quantity = group.Sum(x => x.Quantity),
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = first.Currency.ToLower(),
                            UnitAmount = first.UnitPriceCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = first.TicketType.Title
                            }
                        }
                    };
                })
                .ToList(),

            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = platformFeeCents,
                OnBehalfOf = host.StripeAccountId,
                TransferData = new SessionPaymentIntentDataTransferDataOptions
                {
                    Destination = host.StripeAccountId
                }
            },

            InvoiceCreation = new SessionInvoiceCreationOptions
            {
                Enabled = true,
                InvoiceData = new SessionInvoiceCreationInvoiceDataOptions
                {
                    Metadata = metadata
                }
            },

            Metadata = metadata,

            SuccessUrl = $"{configuration["ClientBaseUrl"]}/checkout/processing?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{configuration["ClientBaseUrl"]}/events?payment=cancelled"
        };

        var service = new SessionService();
        var requestOptions = new RequestOptions { IdempotencyKey = order.Id.ToString() };
        var session = await service.CreateAsync(options, requestOptions, cancellationToken: ct);

        return session.Url;
    }
}
