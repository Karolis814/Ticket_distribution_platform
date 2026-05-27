using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public static class CustomerConfiguration
{
    public static void ConfigureCustomer(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(builder =>
        {
            builder.ToTable("Customers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasIndex(x => x.Email)
                .IsUnique()
                .HasFilter("\"UserId\" IS NOT NULL");

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(x => x.EmailRemindersEnabled)
                .IsRequired();
        });
    }
}
