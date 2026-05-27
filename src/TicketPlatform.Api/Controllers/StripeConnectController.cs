using Microsoft.AspNetCore.Mvc;
using Stripe;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/stripe-connect")]
public class StripeConnectController(
    IHostPaymentSettingsService hostPaymentSettingsService,
    IRepository<User> userRepository,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("onboard/{hostId:guid}")]
    public async Task<IActionResult> CreateOnboardingLink(
        Guid hostId,
        CancellationToken ct)
    {
        var host = await userRepository.GetByIdAsync(hostId, ct);
        if (host is null)
            return NotFound($"User {hostId} not found.");

        var settings = await hostPaymentSettingsService.GetByHostIdAsync(hostId, ct);

        if (settings is null)
        {
            var accountService = new AccountService();

            var account = await accountService.CreateAsync(
                new AccountCreateOptions
                {
                    Type = "express",
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        CardPayments = new AccountCapabilitiesCardPaymentsOptions
                        {
                            Requested = true
                        },
                        Transfers = new AccountCapabilitiesTransfersOptions
                        {
                            Requested = true
                        }
                    }
                },
                cancellationToken: ct);

            settings = new HostPaymentSettings
            {
                HostId = hostId,
                StripeAccountId = account.Id,
                ChargesEnabled = account.ChargesEnabled,
                PayoutsEnabled = account.PayoutsEnabled,
                DetailsSubmitted = account.DetailsSubmitted
            };

            await hostPaymentSettingsService.CreateAsync(settings, ct);
        }

        var accountLinkService = new AccountLinkService();

        var baseUrl = configuration["ClientBaseUrl"];

        var accountLink = await accountLinkService.CreateAsync(
            new AccountLinkCreateOptions
            {
                Account = settings.StripeAccountId,
                Type = "account_onboarding",
                RefreshUrl = $"{baseUrl}/owner/stripe/refresh/{hostId}",
                ReturnUrl = $"{baseUrl}/owner/stripe/return/{hostId}"
            },
            cancellationToken: ct);

        return Ok(new
        {
            url = accountLink.Url
        });
    }

    [HttpGet("status/{hostId:guid}")]
    public async Task<ActionResult<StripeConnectStatusDto>> GetStatus(
        Guid hostId,
        CancellationToken ct)
    {
        var settings = await hostPaymentSettingsService.GetByHostIdAsync(hostId, ct);

        if (settings is null || string.IsNullOrWhiteSpace(settings.StripeAccountId))
        {
            return Ok(new StripeConnectStatusDto(
                hostId,
                null,
                false,
                false,
                false,
                false));
        }

        var accountService = new AccountService();
        var account = await accountService.GetAsync(
            settings.StripeAccountId,
            cancellationToken: ct);

        settings.ChargesEnabled = account.ChargesEnabled;
        settings.PayoutsEnabled = account.PayoutsEnabled;
        settings.DetailsSubmitted = account.DetailsSubmitted;
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        if (account.ChargesEnabled && account.PayoutsEnabled && account.DetailsSubmitted)
            settings.OnboardedAt ??= DateTimeOffset.UtcNow;

        await hostPaymentSettingsService.UpdateAsync(settings, ct);

        var ready = settings.ChargesEnabled &&
                    settings.PayoutsEnabled &&
                    settings.DetailsSubmitted;

        return Ok(new StripeConnectStatusDto(
            hostId,
            settings.StripeAccountId,
            settings.ChargesEnabled,
            settings.PayoutsEnabled,
            settings.DetailsSubmitted,
            ready));
    }
}
