using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/user-settings")]
[Authorize]
public class UserSettingsController(
    IUserSettingsService service,
    IConfiguration configuration) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("User ID not found in token."));

    [HttpGet]
    public async Task<ActionResult<UserSettingsDto>> Get(CancellationToken ct)
    {
        var settings = await service.GetAsync(CurrentUserId, ct);
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpPost("change-email")]
    public async Task<IActionResult> ChangeEmail(
        [FromBody] ChangeEmailRequest request,
        CancellationToken ct)
    {
        var baseUrl = configuration["ClientBaseUrl"]
            ?? throw new InvalidOperationException("ClientBaseUrl is not configured.");

        await service.RequestEmailChangeAsync(CurrentUserId, request.NewEmail, baseUrl, ct);
        return Ok();
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailChangeRequest request,
        CancellationToken ct)
    {
        await service.ConfirmEmailChangeAsync(request.UserId, request.Token, ct);
        return Ok();
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        await service.UpdateProfileAsync(CurrentUserId, request, ct);
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        await service.DeleteAccountAsync(CurrentUserId, ct);
        return Ok();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        try
        {
            await service.ChangePasswordAsync(
                CurrentUserId,
                request.CurrentPassword,
                request.NewPassword,
                ct);

            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}