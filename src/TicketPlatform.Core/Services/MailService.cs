using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace TicketPlatform.Core.Services;

public class MailService(IOptions<SmtpOptions> opts) : IMailService
{
    private readonly SmtpOptions _opts = opts.Value;

    public async Task SendTicketAsync(
        string toEmail,
        string toName,
        string eventName,
        byte[] pdfBytes,
        CancellationToken ct = default)
    {
        using var msg = new MailMessage();
        msg.From = new MailAddress(_opts.From, "Ticket Platform");
        msg.To.Add(new MailAddress(toEmail, toName));
        msg.Subject = $"Your tickets for {eventName}";
        msg.Body =
            $"Hello {toName},\n\nPlease find your tickets for \"{eventName}\" in the attachments below.\n\nEnjoy the event!";

        using var stream = new MemoryStream(pdfBytes);
        msg.Attachments.Add(new Attachment(stream, "tickets.pdf", "application/pdf"));

        using var client = new SmtpClient(_opts.Host, _opts.Port);
        client.EnableSsl = _opts.EnableSsl;
        client.Credentials = _opts.Username is not null
            ? new NetworkCredential(_opts.Username, _opts.Password)
            : null;

        await client.SendMailAsync(msg, ct);
    }
}

public class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string From { get; set; } = "noreply@ticket.dev";
    public bool EnableSsl { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
}
