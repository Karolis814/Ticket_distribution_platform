using Stripe.Checkout;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;

namespace TicketPlatform.Infrastructure.Payments;

public class StripeCheckoutService(
    IHostPaymentSettingsService hostPaymentSettingsService) : IStripeCheckoutService
{
    private const decimal PlatformFeeRate = 0.05m;

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

        var settings = await hostPaymentSettingsService.GetByHostIdAsync(hostId, ct);

        if (settings is null ||
            string.IsNullOrWhiteSpace(settings.StripeAccountId) ||
            !settings.ChargesEnabled)
        {
            throw new InvalidOperationException("Event host has not completed Stripe onboarding.");
        }

        var platformFeeCents = (long)Math.Round(order.TotalPriceCents * PlatformFeeRate);

        var metadata = new Dictionary<string, string>
        {
            { "orderId", order.Id.ToString() },
            { "email", order.Customer.Email },
            { "fullName", $"{order.Customer.FirstName} {order.Customer.LastName}" },
            { "hostId", hostId.ToString() },
            { "stripeAccountId", settings.StripeAccountId }
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
                TransferData = new SessionPaymentIntentDataTransferDataOptions
                {
                    Destination = settings.StripeAccountId
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

            SuccessUrl = "https://localhost:7174/payment-success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = "https://localhost:7174/events?payment=cancelled"
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        return session.Url;
    }
}
