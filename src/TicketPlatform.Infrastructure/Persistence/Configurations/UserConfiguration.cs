using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public static class UserConfiguration
{
    public static void ConfigureUser(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("Users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Username)
                .HasMaxLength(50);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasIndex(x => x.Email)
                .IsUnique();

            builder.Property(x => x.PasswordHash)
                .IsRequired();

            builder.Property(x => x.PasswordSalt)
                .IsRequired();

            builder.Property(x => x.Company)
                .HasMaxLength(200);

            builder.Property(x => x.Address)
                .HasMaxLength(300);

            builder.Property(x => x.TaxCode)
                .HasMaxLength(20);

            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt);

            builder.HasOne(x => x.UserPermissionGroup)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.UserPermissionGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.EmailConfirmed).IsRequired();
            builder.Property(x => x.PendingEmail).HasMaxLength(255);
            builder.Property(x => x.EmailConfirmationTokenHash);
            builder.Property(x => x.EmailConfirmationTokenExpiresAt);
            builder.Property(x => x.EmailRemindersEnabled).IsRequired();
        });
    }
}
