using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public static class UserPermissionGroupConfiguration
{
    public static void ConfigureUserPermissionGroup(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPermissionGroup>(builder =>
        {
            builder.ToTable("UserPermissionGroups");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt);
        });
    }
}
