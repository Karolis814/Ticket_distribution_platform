namespace TicketPlatform.Core.Services;

public interface IMailService
{
    Task SendTicketAsync(string toEmail, string toName, string eventName, byte[] pdfBytes,
        CancellationToken ct = default);
}
