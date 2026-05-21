using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Services;
using TicketPlatform.Infrastructure.Persistence;
using TicketPlatform.Infrastructure.Storage;

namespace TicketPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'Postgres' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IOrderItemService, OrderItemService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<ITicketTypeService, TicketTypeService>();
        services.AddScoped<IHostPaymentSettingsService, HostPaymentSettingsService>();

        services.AddScoped<ITicketPdfService, TicketPdfService>();
        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        services.AddScoped<IMailService, MailService>();

        services.AddScoped<ITicketValidationService, TicketValidationService>();

        services.Configure<BlobStorageOptions>(configuration.GetSection("BlobStorage"));
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            return new BlobServiceClient(opts.ConnectionString);
        });
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        return services;
    }
}
