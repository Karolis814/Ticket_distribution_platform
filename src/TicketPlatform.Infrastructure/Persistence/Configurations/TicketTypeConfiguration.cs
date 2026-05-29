using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public static class TicketTypeConfiguration
{
    public static void ConfigureTicketType(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TicketType>(builder =>
        {
            builder.ToTable("TicketTypes", t =>
            {
                t.HasCheckConstraint("CK_TicketTypes_PriceCents", "\"PriceCents\" >= 0");
                t.HasCheckConstraint("CK_TicketTypes_Quantity", "\"Quantity\" >= 1");
                t.HasCheckConstraint("CK_TicketTypes_MaxUses", "\"MaxUses\" >= 0");
            });


            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.OccurenceStartDate)
                .IsRequired();

            builder.Property(x => x.OccurenceEndDate)
                .IsRequired();

            builder.Property(x => x.AdmissionStartDate)
                .IsRequired();

            builder.Property(x => x.AdmissionEndDate)
                .IsRequired();

            builder.Property(x => x.PriceCents)
                .IsRequired();

            builder.Property(x => x.RowVersion)  // optimistic locking
                .IsRowVersion();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(x => x.MaxUses)
                .IsRequired();

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt);

            builder.HasOne(x => x.Event)
                .WithMany(x => x.TicketTypes)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
