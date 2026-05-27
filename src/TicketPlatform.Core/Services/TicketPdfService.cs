using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Core.Services;

public class TicketPdfService(IOrderService orderService) : ITicketPdfService
{
    public async Task<byte[]> GeneratePdfAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await orderService.GetByIdAsync(orderId, ct);

        if (order is null)
            throw new InvalidOperationException($"Order with ID {orderId} not found.");

        return GeneratePdf(order);
    }

    private static byte[] GeneratePdf(Order order)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var tickets = order.OrderItems.SelectMany(oi => oi.Tickets).ToList();

        if (tickets.Count == 0)
            throw new InvalidOperationException($"Order {order.Id} has no tickets.");

        var pdf = Document.Create(container =>
        {
            foreach (var ticket in tickets)
            {
                var qrPng = GenerateQrPng(ticket.Id.ToString());

                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(11));

                    page.Content().Column(col =>
                    {
                        var @event = ticket.TicketType.Event;
                        var host = @event.Host;
                        var isMultiDay = ticket.TicketType.OccurenceEndDate.Date > ticket.TicketType.OccurenceStartDate.Date;

                        col.Item().Text(@event.Title)
                            .FontSize(22).Bold().AlignCenter();

                        if (!string.IsNullOrWhiteSpace(ticket.TicketType.Title))
                            col.Item().PaddingTop(4)
                                .Text(ticket.TicketType.Title)
                                .FontSize(13).Italic()
                                .FontColor(Colors.Grey.Darken1).AlignCenter();

                        col.Item().PaddingTop(8).AlignCenter().Text(txt =>
                        {
                            txt.DefaultTextStyle(t => t.FontSize(13));

                            if (isMultiDay)
                            {
                                txt.Line($"Starts: {ticket.TicketType.OccurenceStartDate:yyyy-MM-dd HH:mm}");
                                txt.Line($"Ends:   {ticket.TicketType.OccurenceEndDate:yyyy-MM-dd HH:mm}");
                            }
                            else
                            {
                                txt.Span($"{ticket.TicketType.OccurenceStartDate:yyyy-MM-dd}");
                                txt.Span("  ·  ");
                                txt.Span($"{ticket.TicketType.OccurenceStartDate:HH:mm} – {ticket.TicketType.OccurenceEndDate:HH:mm}");
                            }
                        });

                        col.Item().PaddingTop(3).Text(
                                isMultiDay
                                    ? $"Doors open at {ticket.TicketType.AdmissionStartDate:HH:mm} on {ticket.TicketType.AdmissionStartDate:yyyy-MM-dd}"
                                    : $"Doors open at {ticket.TicketType.AdmissionStartDate:HH:mm}")
                            .FontSize(9).FontColor(Colors.Grey.Medium).AlignCenter();

                        if (!string.IsNullOrEmpty(@event.Location))
                            col.Item().PaddingTop(4).Text(@event.Location)
                                .FontColor(Colors.Grey.Medium).AlignCenter();

                        col.Item().PaddingTop(20).AlignCenter()
                            .Width(160).Height(160)
                            .Image(qrPng);

                        col.Item().PaddingTop(0)
                            .Text($"Ticket ID: {ticket.Id}")
                            .FontSize(7).FontColor(Colors.Grey.Medium).AlignCenter();

                        col.Item().PaddingTop(4)
                            .Text($"Order ID: {order.Id}")
                            .FontSize(7).FontColor(Colors.Grey.Medium).AlignCenter();

                        col.Item().PaddingTop(8)
                            .Text(
                                $"Price: {ticket.OrderItem.UnitPriceCents / 100m:F2} {ticket.OrderItem.Currency.ToUpper()}")
                            .FontSize(10).Bold().AlignCenter();

                        col.Item().PaddingTop(40)
                            .LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        col.Item().PaddingTop(8).Row(row =>
                        {
                            // Organizer
                            row.RelativeItem().Column(c =>
                            {
                                c.Spacing(3);
                                c.Item().Text("Organizer").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);

                                if (!string.IsNullOrWhiteSpace(host.Company))
                                    c.Item().Text(host.Company).FontSize(8).Bold().FontColor(Colors.Grey.Darken1);

                                if (!string.IsNullOrWhiteSpace(host.Address))
                                    c.Item().Text(host.Address).FontSize(8).FontColor(Colors.Grey.Medium);

                                if (!string.IsNullOrWhiteSpace(host.TaxCode))
                                    c.Item().Text($"Tax / Company ID: {host.TaxCode}").FontSize(8).FontColor(Colors.Grey.Medium);

                                if (string.IsNullOrWhiteSpace(host.Company) &&
                                    string.IsNullOrWhiteSpace(host.Address) &&
                                    string.IsNullOrWhiteSpace(host.TaxCode))
                                    c.Item().Text("No organizer details provided.").FontSize(8).FontColor(Colors.Grey.Lighten1);
                            });

                            row.ConstantItem(1).Background(Colors.Grey.Lighten3);

                            // Contact
                            row.RelativeItem().PaddingLeft(10).Column(c =>
                            {
                                c.Spacing(3);
                                c.Item().Text("Contact").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);

                                var fullName = $"{host.FirstName} {host.LastName}".Trim();
                                if (!string.IsNullOrWhiteSpace(fullName))
                                    c.Item().Text(fullName).FontSize(8).Bold().FontColor(Colors.Grey.Darken1);

                                if (!string.IsNullOrWhiteSpace(host.Email))
                                    c.Item().Text(host.Email).FontSize(8).FontColor(Colors.Grey.Medium);

                                if (!string.IsNullOrWhiteSpace(host.PhoneNumber))
                                    c.Item().Text(host.PhoneNumber).FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                        });
                    });
                });
            }
        });

        return pdf.GeneratePdf();
    }

    private static byte[] GenerateQrPng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        using var code = new PngByteQRCode(data);
        return code.GetGraphic(6);
    }
}
