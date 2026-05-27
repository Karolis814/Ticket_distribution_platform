using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Blazored.LocalStorage;
using Radzen;
using TicketPlatform.Web;
using TicketPlatform.Web.Services;
using Microsoft.Extensions.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
                 ?? throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json.");

builder.Services.AddRadzenComponents();

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();

builder.Services.AddTransient<AuthTokenHandler>();

builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();

builder.Services.AddHttpClient("AuthorizedClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("AuthorizedClient");
});

builder.Services.AddScoped<IImagesClient, ImagesClient>();
builder.Services.AddScoped<IEventsClient, EventsClient>();
builder.Services.AddScoped<IAuthClient, AuthClient>();
builder.Services.AddScoped<IOrdersClient, OrdersClient>();

builder.Services.AddRadzenComponents();

builder.Services.AddScoped<IEventsClient>(sp => new EventsClient(
    sp.GetRequiredService<HttpClient>(),
    sp.GetRequiredService<NotificationService>()));

builder.Services.AddScoped<IPlacesClient>(sp =>
    new PlacesClient(sp.GetRequiredService<HttpClient>()));

builder.Services.AddScoped<IImagesClient, ImagesClient>();

builder.Services.AddScoped<IUsersClient, UsersClient>();

builder.Services.AddScoped<IHostPaymentsClient, HostPaymentsClient>();

builder.Services.AddScoped<IUserSettingsClient, UserSettingsClient>();

await builder.Build().RunAsync();
