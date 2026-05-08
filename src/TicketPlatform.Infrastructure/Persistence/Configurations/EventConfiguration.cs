using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketPlatform.Core.Events;
using TicketPlatform.Core.Tickets;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public static class EventConfiguration
{
    public static void ConfigureEvent(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(builder =>
        {
            builder.ToTable("Events");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);
            builder.Property(x => x.StartsAt)
                .IsRequired();

            builder.Property(x => x.EndsAt)
                .IsRequired();

            builder.Property(x => x.TicketCount)
                .IsRequired();  
            builder.Property(x => x.CreatedAt)
                .IsRequired();
            builder.Property(x => x.UpdatedAt);
            builder.HasMany(x => x.Tickets)
                .WithOne(x => x.Event)
                .HasForeignKey(x => x.EventId);
            
            builder.HasOne(x => x.Host)
                .WithMany(x => x.HostedEvents)
                .HasForeignKey(x => x.HostId);

        });

        modelBuilder.Entity<Ticket>(builder =>
        {
            builder.ToTable("Tickets");

            builder.HasKey(x => x.Id);


            builder.Property(x => x.Price)
                .IsRequired();

            builder.Property(x => x.Currency)
                .HasMaxLength(3);
            builder.Property(x => x.CreatedAt)
                .IsRequired();
            builder.Property(x => x.UpdatedAt);
            
            builder.Property(x => x.Status)
                .IsRequired();
            builder.Property(x => x.admisionStart)
                .IsRequired();

            builder.Property(x => x.admisionEnd)
                .IsRequired();
            
            
        });
    }
}