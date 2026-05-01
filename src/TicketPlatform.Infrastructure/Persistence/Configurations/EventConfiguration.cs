using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketPlatform.Core.Events;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Location).IsRequired().HasMaxLength(200);
        builder.Property(e => e.StartsAt).IsRequired();
        builder.Property(e => e.Capacity).IsRequired();

        builder.Property(e => e.Version).IsRowVersion();

        builder.HasIndex(e => e.StartsAt);
    }
}
