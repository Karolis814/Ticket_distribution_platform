using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Mail.Templates;
using TicketPlatform.Core.Services;
using TicketPlatform.Infrastructure.Persistence;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Infrastructure.Reminders;

public class EventReminderJob(
    IServiceScopeFactory scopeFactory,
    ILogger<EventReminderJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Event reminder tick failed");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mail = scope.ServiceProvider.GetRequiredService<IMailService>();

        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddHours(24).AddMinutes(15);
        var skipBefore = now.AddMinutes(30);

        var due = await db.OrderItems
            .Include(oi => oi.TicketType).ThenInclude(tt => tt.Event)
            .Include(oi => oi.Order).ThenInclude(o => o.Customer)
            .Where(oi => oi.ReminderStatus == ReminderStatus.Pending
                      && oi.TicketType.OccurenceStartDate < cutoff)
            .ToListAsync(ct);

        if (due.Count == 0) return;

        var stale = due.Where(oi => oi.TicketType.OccurenceStartDate < skipBefore).ToList();
        var sendable = due.Where(oi => oi.TicketType.OccurenceStartDate >= skipBefore).ToList();

        if (stale.Count > 0)
        {
            foreach (var oi in stale) oi.ReminderStatus = ReminderStatus.Sent;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Skipped {Count} stale reminders", stale.Count);
        }

        var groups = sendable.GroupBy(oi => new
        {
            oi.Order.CustomerId,
            EventId = oi.TicketType.EventId,
            Start = oi.TicketType.OccurenceStartDate
        });

        foreach (var group in groups)
        {
            var sample = group.First();
            var customer = sample.Order.Customer;
            var ev = sample.TicketType.Event;
            var items = group
                .Select(oi => (oi.TicketType.Title, oi.Quantity))
                .ToList();

            try
            {
                var message = EmailTemplates.EventReminder(
                    customer.Email,
                    $"{customer.FirstName} {customer.LastName}",
                    ev.Title,
                    sample.TicketType.OccurenceStartDate,
                    items);

                await mail.SendAsync(message, ct);

                foreach (var oi in group) oi.ReminderStatus = ReminderStatus.Sent;
                await db.SaveChangesAsync(ct);

                logger.LogInformation(
                    "Sent reminder to {Email} for event {EventId} start {Start:O}",
                    customer.Email, group.Key.EventId, group.Key.Start);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send reminder to {Email} for event {EventId}",
                    customer.Email, group.Key.EventId);
            }
        }
    }
}
