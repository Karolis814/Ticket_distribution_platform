using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TicketPlatform.Core.Models;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScanController(ITicketValidationService validationService) : ControllerBase
{
    [Authorize(Roles = "Host")]
    [HttpGet("{ticketId:guid}")]
    public async Task<ActionResult<TicketValidationResultDto>> Validate(
        Guid ticketId,
        CancellationToken ct)
    {

        var user = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (user is null)
            return Unauthorized();
          Guid userId = Guid.Parse(user);


        TimeZoneInfo? userTz = null;
        var tzHeader = Request.Headers["X-Timezone"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(tzHeader))
        {
            try { userTz = TimeZoneInfo.FindSystemTimeZoneById(tzHeader); }
            catch { /* unknown timezone — fall back to UTC */ }
        }

        var result = await validationService.ValidateAsync(ticketId, userId, userTz, ct);
        return Ok(MapToDto(result));
    }

    private static TicketValidationResultDto MapToDto(TicketValidationResult r) =>
        new(
            Status: (ValidationStatus)(int)r.Status,
            EventName: r.EventName,
            OccurenceStartDate: r.OccurenceStartDate,
            OccurenceEndDate: r.OccurenceEndDate,
            AdmissionStartDate: r.AdmissionStartDate,
            AdmissionEndDate: r.AdmissionEndDate,
            TicketTypeName: r.TicketTypeName,
            TimesUsed: r.TimesUsed,
            MaxUses: r.MaxUses,
            Message: r.Message
        );
}
