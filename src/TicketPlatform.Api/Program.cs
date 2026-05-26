using Serilog;
using Stripe;
using TicketPlatform.Api.Middleware;
using TicketPlatform.Core.Services;
using TicketPlatform.Infrastructure;
using TicketPlatform.Infrastructure.Payments;
using TicketPlatform.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);

StripeConfiguration.ApiKey =
    builder.Configuration["Stripe:SecretKey"];

builder.Services.AddScoped<
    IStripeCheckoutService,
    StripeCheckoutService>();

builder.Services.AddScoped<
    IUserSettingsService,
    UserSettingsService>();

builder.Services.Configure<GooglePlacesOptions>(
    builder.Configuration.GetSection("GooglePlacesOptions"));

builder.Services.AddHttpClient<
    IPlacesService,
    GooglePlacesService>();

const string blazorCors = "BlazorClient";

builder.Services.AddCors(options =>
{
    options.AddPolicy(blazorCors, policy => policy
        .WithOrigins(
            builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(blazorCors);
app.UseAuthorization();
app.MapControllers();

app.Run();

public abstract partial class Program
{
}
