using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace TicketPlatform.Core.Services;

public class MailService(IOptions<SmtpOptions> opts) : IMailService
{
    private readonly SmtpOptions _opts = opts.Value;

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken ct = default)
    {
        var mime = new MimeMessage();

        mime.From.Add(new MailboxAddress(
            "Ticket Platform",
            _opts.From));

        mime.To.Add(new MailboxAddress(
            message.ToName,
            message.To));

        mime.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            TextBody = message.BodyText,
            HtmlBody = message.BodyHtml
        };

        if (message.Attachments is not null)
        {
            foreach (var attachment in message.Attachments)
            {
                builder.Attachments.Add(
                    attachment.FileName,
                    attachment.Content,
                    ContentType.Parse(attachment.ContentType));
            }
        }

        mime.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        var secureSocket = _opts.EnableSsl
            ? SecureSocketOptions.StartTlsWhenAvailable
            : SecureSocketOptions.None;

        await client.ConnectAsync(
            _opts.Host,
            _opts.Port,
            secureSocket,
            ct);

        if (!string.IsNullOrWhiteSpace(_opts.Username))
        {
            await client.AuthenticateAsync(
                _opts.Username,
                _opts.Password ?? string.Empty,
                ct);
        }

        await client.SendAsync(mime, ct);

        await client.DisconnectAsync(true, ct);
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
