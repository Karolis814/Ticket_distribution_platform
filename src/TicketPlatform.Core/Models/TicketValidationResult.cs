using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Core.Models;

public record TicketValidationResult(
    ValidationStatus Status,
    string? EventName,
    DateTimeOffset? OccurenceStartDate,
    DateTimeOffset? OccurenceEndDate,
    DateTimeOffset? AdmissionStartDate,
    DateTimeOffset? AdmissionEndDate,
    string? TicketTypeName,
    int? TimesUsed,
    int? MaxUses,
    string Message
);
