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

// Register HttpClient with the base address
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Register IEventsClient with HttpClient
builder.Services.AddScoped<IEventsClient>(sp => new EventsClient(sp.GetRequiredService<HttpClient>()));

// Register IPlacesClient with HttpClient
builder.Services.AddScoped<IPlacesClient>(sp => new PlacesClient(sp.GetRequiredService<HttpClient>()));

builder.Services.AddRadzenComponents();
builder.Services.AddScoped<IEventsClient, EventsClient>();
await builder.Build().RunAsync();
