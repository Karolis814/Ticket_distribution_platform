namespace TicketPlatform.Shared.Dtos;

public record UserSettingsDto(
    Guid UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Company,
    string? Address,
    string? TaxCode
);

public record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Company,
    string? Address,
    string? TaxCode
);

public record ChangeEmailRequest(string NewEmail);

public record ConfirmEmailChangeRequest(Guid UserId, string Token);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);