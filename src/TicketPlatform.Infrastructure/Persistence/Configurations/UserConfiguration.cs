using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Users;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;

public static class UserConfiguration
{
    public static void ConfigureUsers(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("Users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasOne(x => x.UserPermissionGroup)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.UserPermissionGroupId);

            builder.HasMany(x => x.HostedEvents)
                .WithOne(x => x.Host)
                .HasForeignKey(x => x.HostId);
        });

        modelBuilder.Entity<UserPermissionGroup>(builder =>
        {
            builder.ToTable("UserPermissionGroups");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.HasMany(x => x.Permissions)
                .WithMany(x => x.UserPermissionGroups)
                .UsingEntity(j => j.ToTable("UserPermissionGroupPermissions"));
        });

        modelBuilder.Entity<Permission>(builder =>
        {
            builder.ToTable("Permisions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.PermissionStatus)
                .IsRequired();
        });
    }
}