namespace TicketPlatform.Shared.Dtos;

public record UserSettingsDto(
    Guid UserId,
    string Email,
    bool EmailConfirmed,
    string? PendingEmail,
    bool EmailRemindersEnabled
);

public record ChangeEmailRequest(Guid UserId, string NewEmail);

public record ConfirmEmailChangeRequest(Guid UserId, string Token);

public record ChangePasswordRequest(
    Guid UserId,
    string CurrentPassword,
    string NewPassword
);

public record UpdateEmailRemindersRequest(
    Guid UserId,
    bool EmailRemindersEnabled
);
