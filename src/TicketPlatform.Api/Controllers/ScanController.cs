using Microsoft.AspNetCore.Mvc;
using TicketPlatform.Core.Models;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScanController(ITicketValidationService validationService) : ControllerBase
{
    [HttpGet("{ticketId:guid}")]
    public async Task<ActionResult<TicketValidationResultDto>> Validate(
        Guid ticketId,
        CancellationToken ct)
    {
        var result = await validationService.ValidateAsync(ticketId, ct);
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
