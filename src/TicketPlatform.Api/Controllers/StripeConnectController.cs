using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/stripe-connect")]
[Authorize]
public class StripeConnectController(
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

        if (string.IsNullOrWhiteSpace(host.StripeAccountId))
        {
            var account = await new AccountService().CreateAsync(
                new AccountCreateOptions
                {
                    Type = "express",
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                        Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                    }
                },
                cancellationToken: ct);

            host.StripeAccountId = account.Id;
            host.UpdatedAt = DateTimeOffset.UtcNow;
            userRepository.Update(host);
            await userRepository.SaveChangesAsync(ct);
        }

        var baseUrl = configuration["ClientBaseUrl"];

        var accountLink = await new AccountLinkService().CreateAsync(
            new AccountLinkCreateOptions
            {
                Account = host.StripeAccountId,
                Type = "account_onboarding",
                RefreshUrl = $"{baseUrl}/stripe/refresh/{hostId}",
                ReturnUrl = $"{baseUrl}/stripe/return/{hostId}"
            },
            cancellationToken: ct);

        return Ok(new { url = accountLink.Url });
    }

    [HttpGet("status/{hostId:guid}")]
    public async Task<ActionResult<StripeConnectStatusDto>> GetStatus(
        Guid hostId,
        CancellationToken ct)
    {
        var host = await userRepository.GetByIdAsync(hostId, ct);

        if (host is null || string.IsNullOrWhiteSpace(host.StripeAccountId))
            return Ok(new StripeConnectStatusDto(hostId, null, false));

        var account = await new AccountService().GetAsync(host.StripeAccountId, cancellationToken: ct);
        var ready = account.ChargesEnabled && account.PayoutsEnabled;

        if (ready && host.StripeOnboardedAt is null)
        {
            host.Role = UserRole.Host;
            host.StripeOnboardedAt = DateTimeOffset.UtcNow;
            host.UpdatedAt = DateTimeOffset.UtcNow;
            userRepository.Update(host);
            await userRepository.SaveChangesAsync(ct);
        }

        return Ok(new StripeConnectStatusDto(hostId, host.StripeAccountId, ready));
    }
}