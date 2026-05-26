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
        Attachments:
        [
            new EmailAttachment("tickets.pdf", "application/pdf", pdf)
        ]);

    public static EmailMessage ConfirmEmailChange(
        string toEmail,
        string toName,
        string confirmationUrl) => new(
        To: toEmail,
        ToName: toName,
        Subject: "Confirm your new email address",
        BodyText:
            $"Hello {toName},\n\n" +
            "Please confirm your new email address by opening this link:\n\n" +
            $"{confirmationUrl}\n\n" +
            "If you did not request this change, ignore this email.");
}
