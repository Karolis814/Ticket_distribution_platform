using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Models;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Core.Services;

public class TicketValidationService(ITicketService ticketService) : ITicketValidationService
{
    public async Task<TicketValidationResult> ValidateAsync(Guid ticketId, Guid userId, TimeZoneInfo? timeZone = null, CancellationToken ct = default)
    {
        var ticket = await ticketService.GetByIdAsync(ticketId, ct);

        if (ticket is null)
            return new TicketValidationResult(
                ValidationStatus.NotFound,
                null, null, null, null, null, null, null, null,
                "Ticket not found.");
        else if (ticket.TicketType.Event.HostId != userId)
        {
             return new TicketValidationResult(
                ValidationStatus.NotFound,
                null, null, null, null, null, null, null, null,
                "Only host can validate tickets.");

        }

        var result = Evaluate(ticket, timeZone);
        await ticketService.UpdateAsync(ticket, ct);
        return result;
    }

    private static DateTimeOffset Local(DateTimeOffset utc, TimeZoneInfo? tz) =>
        tz is null ? utc : TimeZoneInfo.ConvertTime(utc, tz);

    private static string TzSuffix(DateTimeOffset local)
    {
        if (local.Offset == TimeSpan.Zero) return "UTC";
        var h = local.Offset.Hours;
        var m = Math.Abs(local.Offset.Minutes);
        return m == 0 ? $"UTC{h:+0;-0}" : $"UTC{h:+0;-0}:{m:D2}";
    }

    private static TicketValidationResult Evaluate(Ticket ticket, TimeZoneInfo? tz)
    {
        var now = DateTimeOffset.UtcNow;
        var admStart = Local(ticket.TicketType.AdmissionStartDate, tz);
        var admEnd   = Local(ticket.TicketType.AdmissionEndDate, tz);

        if (now < ticket.TicketType.AdmissionStartDate)
            return new TicketValidationResult(
                ValidationStatus.AdmissionNotStarted,
                ticket.TicketType.Event.Title,
                ticket.TicketType.OccurenceStartDate,
                ticket.TicketType.OccurenceEndDate,
                ticket.TicketType.AdmissionStartDate,
                ticket.TicketType.AdmissionEndDate,
                ticket.TicketType.Title,
                ticket.TimesUsed,
                ticket.TicketType.MaxUses,
                $"Admission hasn't started yet. Doors open at {admStart:HH:mm} on {admStart:dd MMM} {TzSuffix(admStart)}.");

        if (now > ticket.TicketType.AdmissionEndDate)
            return new TicketValidationResult(
                ValidationStatus.AdmissionEnded,
                ticket.TicketType.Event.Title,
                ticket.TicketType.OccurenceStartDate,
                ticket.TicketType.OccurenceEndDate,
                ticket.TicketType.AdmissionStartDate,
                ticket.TicketType.AdmissionEndDate,
                ticket.TicketType.Title,
                ticket.TimesUsed,
                ticket.TicketType.MaxUses,
                $"Admission ended at {admEnd:HH:mm} on {admEnd:dd MMM} {TzSuffix(admEnd)}.");

        if (ticket.TicketType.MaxUses > 0 && ticket.TimesUsed >= ticket.TicketType.MaxUses)
            return new TicketValidationResult(
                ValidationStatus.MaxUsesReached,
                ticket.TicketType.Event.Title,
                ticket.TicketType.OccurenceStartDate,
                ticket.TicketType.OccurenceEndDate,
                ticket.TicketType.AdmissionStartDate,
                ticket.TicketType.AdmissionEndDate,
                ticket.TicketType.Title,
                ticket.TimesUsed,
                ticket.TicketType.MaxUses,
                $"Ticket has already been used the maximum number of times! ({ticket.TimesUsed}/{ticket.TicketType.MaxUses})");

        ticket.TimesUsed++;

        return new TicketValidationResult(
            ValidationStatus.Ok,
            ticket.TicketType.Event.Title,
            ticket.TicketType.OccurenceStartDate,
            ticket.TicketType.OccurenceEndDate,
            ticket.TicketType.AdmissionStartDate,
            ticket.TicketType.AdmissionEndDate,
            ticket.TicketType.Title,
            ticket.TimesUsed,
            ticket.TicketType.MaxUses,
            "Valid ticket. Enjoy the event!");
    }
}
