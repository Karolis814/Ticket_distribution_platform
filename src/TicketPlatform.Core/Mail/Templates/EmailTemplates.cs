using TicketPlatform.Core.Services;

namespace TicketPlatform.Core.Mail.Templates;

public static class EmailTemplates
{
    public static EmailMessage TicketDelivery(
        string toEmail,
        string toName,
        string eventTitle,
        byte[] pdf) => new(
        To: toEmail,
        ToName: toName,
        Subject: $"Your tickets for {eventTitle}",
        BodyText:
            $"Hello {toName},\n\n" +
            $"Please find your tickets for \"{eventTitle}\" in the attachments below.\n\n" +
            "Enjoy the event!",
        Attachments: [new EmailAttachment("tickets.pdf", "application/pdf", pdf)]);
}
