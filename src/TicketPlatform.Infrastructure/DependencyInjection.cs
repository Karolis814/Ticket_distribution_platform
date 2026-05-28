using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Services;
using TicketPlatform.Infrastructure.Logging;
using TicketPlatform.Infrastructure.Persistence;
using TicketPlatform.Infrastructure.Services;
using TicketPlatform.Infrastructure.Storage;
using TicketPlatform.Core.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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

        services.AddBusinessLogicInterception();

        services.AddInterceptedScoped<ICustomerService, CustomerService>(configuration);
        services.AddInterceptedScoped<IEventService, EventService>(configuration);
        services.AddInterceptedScoped<IOrderItemService, OrderItemService>(configuration);
        services.AddInterceptedScoped<IOrderService, OrderService>(configuration);
        services.AddInterceptedScoped<ITicketService, TicketService>(configuration);
        services.AddInterceptedScoped<ITicketTypeService, TicketTypeService>(configuration);
        services.AddInterceptedScoped<ITicketPdfService, TicketPdfService>(configuration);
        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        services.AddInterceptedScoped<IMailService, MailService>(configuration);

        services.AddInterceptedScoped<ITicketValidationService, TicketValidationService>(configuration);

        // Add Google Places API service
        services.Configure<GooglePlacesOptions>(configuration.GetSection("GooglePlaces"));
        services.AddScoped<IPlacesService>(sp =>
            new GooglePlacesService(
                new HttpClient(),
                sp.GetRequiredService<IOptions<GooglePlacesOptions>>(),
                sp.GetRequiredService<ILogger<GooglePlacesService>>()
            )
        );
        services.Configure<BlobStorageOptions>(configuration.GetSection("BlobStorage"));
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            return new BlobServiceClient(opts.ConnectionString);
        });
        services.AddInterceptedScoped<IBlobStorageService, AzureBlobStorageService>(configuration);
        services.AddInterceptedScoped<IPasswordService, PasswordService>(configuration);
        services.AddInterceptedScoped<IUserService, UserService>(configuration);
        services.Configure<JWTSettings>(configuration.GetSection("JwtSettings"));
        services.AddInterceptedScoped<IJWTService, JWTService>(configuration);

        
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JWTSettings>()
            ?? throw new InvalidOperationException("JwtSettings section is missing from configuration.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings.Issuer,
            ValidAudience            = jwtSettings.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew                = TimeSpan.Zero  
        };
    });
        return services;
    }
}
