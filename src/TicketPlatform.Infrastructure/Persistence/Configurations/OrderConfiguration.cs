using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public static class OrderConfiguration
{
    public static void ConfigureOrder(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("Orders", t =>
                t.HasCheckConstraint("CK_Orders_TotalPriceCents", "\"TotalPriceCents\" >= 0"));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TotalPriceCents)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt);

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
