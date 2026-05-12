using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public static class PaymentConfiguration
{
    public static void ConfigurePayment(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(builder =>
        {
            builder.ToTable("Payments", t =>
                t.HasCheckConstraint("CK_Payments_AmountCents", "\"AmountCents\" >= 0"));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AmountCents)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(x => x.StripePaymentIntentId)
                .HasMaxLength(255);

            builder.Property(x => x.StripeCheckoutSessionId)
                .HasMaxLength(255);

            builder.Property(x => x.StripeStatus)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.SucceededAt);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt);

            builder.HasOne(x => x.Order)
                .WithOne(x => x.Payment)
                .HasForeignKey<Payment>(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
