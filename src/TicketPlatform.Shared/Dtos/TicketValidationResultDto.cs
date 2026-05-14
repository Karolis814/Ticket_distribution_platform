using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Shared.Dtos;

public record TicketValidationResultDto(
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
