using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Entities;
using TicketPlatform.Infrastructure.Persistence.Configurations;

namespace TicketPlatform.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPermissionGroup> UserPermissionGroups => Set<UserPermissionGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureCustomer();
        modelBuilder.ConfigureEvent();
        modelBuilder.ConfigureOrder();
        modelBuilder.ConfigureOrderItem();
        modelBuilder.ConfigurePayment();
        modelBuilder.ConfigurePermission();
        modelBuilder.ConfigureTicket();
        modelBuilder.ConfigureTicketType();
        modelBuilder.ConfigureUser();
        modelBuilder.ConfigureUserPermissionGroup();
    }
}
