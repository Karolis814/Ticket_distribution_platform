using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using TicketPlatform.Web;
using TicketPlatform.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
                 ?? throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json.");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddRadzenComponents();

builder.Services.AddScoped<IEventsClient>(sp => new EventsClient(
    sp.GetRequiredService<HttpClient>(),
    sp.GetRequiredService<NotificationService>()));

builder.Services.AddScoped<IPlacesClient>(sp => new PlacesClient(sp.GetRequiredService<HttpClient>()));

builder.Services.AddScoped<IImagesClient, ImagesClient>();

builder.Services.AddScoped<IUsersClient, UsersClient>();

await builder.Build().RunAsync();
