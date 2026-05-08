using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketPlatform.Core.OrderItems;
using TicketPlatform.Core.Orders;
using TicketPlatform.Core.Customers;

namespace TicketPlatform.Infrastructure.Persistence.Configurations;
public static class OrderConfiguration
{
    
    public static void ConfigureOrder(this ModelBuilder modelBuilder)
    {
        
        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("Orders");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TotalPrice)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(5);

            builder.Property(x => x.Status)
                .IsRequired();
            
            builder.HasMany(x => x.OrderItems)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId);
            
            builder.HasOne(x => x.Customer)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.ToTable("OrderItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PriceAtPurchase)
                .IsRequired();


        });

        modelBuilder.Entity<Customer>(builder =>
        {
            builder.ToTable("Customers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .IsRequired(false);
        });

    }


}