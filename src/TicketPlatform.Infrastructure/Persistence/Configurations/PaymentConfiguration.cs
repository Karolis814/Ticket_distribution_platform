using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;
public static class PaymentConfiguration
{

    public static void ConfigurePayment (this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(builder =>
        {
            builder.ToTable("Payments");
            builder.HasKey(x => x.PaymentId);

            builder.HasOne(x => x.Order)
                .WithOne(x => x.Payment);

            builder.Property(x => x.StripePaymentIntentId)
                .IsRequired();

            builder.Property(x => x.StripePaymentIntentId)
                .IsRequired();
            builder.Property(x => x.Currency)
                .IsRequired();
            builder.Property(x => x.Amount)
                .IsRequired();
            builder.Property(x => x.Status)
                .IsRequired();
            builder.Property(x => x.CreatedAt)
                .IsRequired();
            builder.Property(x => x.UpdatedAt)
                .IsRequired();
            builder.Property(x => x.SucceededAt)
                .IsRequired();
        });
    }


}
