using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public static class HostPaymentSettingsConfiguration
{
    public static void ConfigureHostPaymentSettings(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HostPaymentSettings>(builder =>
        {
            builder.ToTable("HostPaymentSettings");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.StripeAccountId)
                .HasMaxLength(255);

            builder.Property(x => x.ChargesEnabled)
                .IsRequired();

            builder.Property(x => x.PayoutsEnabled)
                .IsRequired();

            builder.Property(x => x.DetailsSubmitted)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt);

            builder.Property(x => x.OnboardedAt);

            builder.HasIndex(x => x.HostId)
                .IsUnique();

            builder.HasOne(x => x.Host)
                .WithOne()
                .HasForeignKey<HostPaymentSettings>(x => x.HostId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
