using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public static class OrderItemConfiguration
{
    public static void ConfigureOrderItem(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.ToTable("OrderItems", t =>
            {
                t.HasCheckConstraint("CK_OrderItems_Quantity", "\"Quantity\" >= 1");
                t.HasCheckConstraint("CK_OrderItems_UnitPriceCents", "\"UnitPriceCents\" >= 0");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.UnitPriceCents)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(x => x.ReminderStatus)
                .IsRequired()
                .HasConversion<int>();

            builder.HasIndex(x => x.ReminderStatus);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt);

            builder.HasOne(x => x.Order)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.TicketType)
                .WithMany()
                .HasForeignKey(x => x.TicketTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Tickets)
                .WithOne(x => x.OrderItem)
                .HasForeignKey(x => x.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
