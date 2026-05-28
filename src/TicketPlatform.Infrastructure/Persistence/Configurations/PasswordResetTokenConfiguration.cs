using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public static class PasswordResetTokenConfiguration
{
    public static void ConfigurePasswordResetToken(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PasswordResetToken>(builder =>
        {
            builder.ToTable("PasswordResetTokens");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.TokenHash)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.Property(x => x.UsedAt);
        });
    }
}