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

    public static EmailMessage EventReminder(
        string toEmail,
        string toName,
        string eventTitle,
        DateTimeOffset occurenceStart,
        IReadOnlyList<(string Title, int Quantity)> items)
    {
        var lines = string.Join("\n", items.Select(i => $" - {i.Title} x{i.Quantity}"));

        return new EmailMessage(
            To: toEmail,
            ToName: toName,
            Subject: $"Reminder: {eventTitle} is tomorrow",
            BodyText:
                $"Hello {toName},\n\n" +
                $"This is a reminder that \"{eventTitle}\" starts at " +
                $"{occurenceStart.UtcDateTime:yyyy-MM-dd HH:mm} UTC.\n\n" +
                $"Your tickets:\n{lines}\n\n" +
                "See you there!");
    }

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

    
    public static EmailMessage PasswordResetEmail(string toEmail, string toName, string resetUrl){

        return new EmailMessage(
            To: toEmail,
            ToName: toName,
            Subject: "Reset your password",
            BodyText:
                $"Hello {toName},\n\n" +
                "We received a request to reset your password.\n\n" +
                "You can reset it using the link below:\n\n" +
                $"{resetUrl}\n\n" +
                "If you did not request this, you can safely ignore this email.");
    }
        
        public static EmailMessage ConfirmEmail(
            string toEmail,
            string toName,
            string confirmationUrl) {

              return  new EmailMessage(
            To: toEmail,
            ToName: toName,
            Subject: "Confirm your email address",
            BodyText:
                $"Hello {toName},\n\n" +
                "Please confirm your email address by clicking the link below:\n\n" +
                $"{confirmationUrl}\n\n" +
                "If you did not create an account, you can ignore this email.");
            } 
        



    }

