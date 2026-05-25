using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Entities;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public static class PermissionConfiguration
{
    public static void ConfigurePermission(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Permission>(builder =>
        {
            builder.ToTable("Permissions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.Title)
                .IsUnique();
            
            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt);

            builder.HasMany(x => x.UserPermissionGroups)
                .WithMany(x => x.Permissions)
                .UsingEntity(j => j.ToTable("UserPermissionGroupPermissions"));
        });
    }
}
