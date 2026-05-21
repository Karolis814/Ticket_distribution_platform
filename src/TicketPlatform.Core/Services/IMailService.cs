namespace TicketPlatform.Core.Services;

public interface IMailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public record EmailMessage(
    string To,
    string ToName,
    string Subject,
    string BodyText,
    string? BodyHtml = null,
    IReadOnlyList<EmailAttachment>? Attachments = null);

public record EmailAttachment(
    string FileName,
    string ContentType,
    byte[] Content);
