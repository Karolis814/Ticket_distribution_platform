using Microsoft.EntityFrameworkCore;
using TicketPlatform.Core.Entities;
using TicketPlatform.Infrastructure.Persistence.Configurations;
namespace TicketPlatform.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPermissionGroup> UserPermissionGroups => Set<UserPermissionGroup>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Payment> Payments => Set<Payment>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureEvent();
        modelBuilder.ConfigureOrder();
        modelBuilder.ConfigurePayment();
        modelBuilder.ConfigureUsers();

        base.OnModelCreating(modelBuilder);
    }
}
