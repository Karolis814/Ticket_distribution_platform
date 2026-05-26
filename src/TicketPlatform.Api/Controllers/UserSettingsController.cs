using Microsoft.AspNetCore.Mvc;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/user-settings")]
public class UserSettingsController(IUserSettingsService service) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserSettingsDto>> Get(
        Guid userId,
        CancellationToken ct)
    {
        var settings = await service.GetAsync(userId, ct);
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpPost("change-email")]
    public async Task<IActionResult> ChangeEmail(
        [FromBody] ChangeEmailRequest request,
        CancellationToken ct)
    {
        var baseUrl = "https://localhost:7174";

        await service.RequestEmailChangeAsync(
            request.UserId,
            request.NewEmail,
            baseUrl,
            ct);

        return Ok();
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailChangeRequest request,
        CancellationToken ct)
    {
        await service.ConfirmEmailChangeAsync(
            request.UserId,
            request.Token,
            ct);

        return Ok();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        await service.ChangePasswordAsync(
            request.UserId,
            request.CurrentPassword,
            request.NewPassword,
            ct);

        return Ok();
    }

    [HttpPost("email-reminders")]
    public async Task<IActionResult> UpdateEmailReminders(
        [FromBody] UpdateEmailRemindersRequest request,
        CancellationToken ct)
    {
        await service.UpdateEmailRemindersAsync(
            request.UserId,
            request.EmailRemindersEnabled,
            ct);

        return Ok();
    }
}
