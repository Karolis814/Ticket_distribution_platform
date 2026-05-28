using Castle.DynamicProxy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TicketPlatform.Infrastructure.Logging;

public static class InterceptedServiceCollectionExtensions
{
    public const string InterceptionToggleKey = "Audit:InterceptBusinessLogic";

    public static IServiceCollection AddBusinessLogicInterception(
        this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<ProxyGenerator>();
        services.TryAddSingleton<LoggingInterceptor>();
        return services;
    }

    public static IServiceCollection AddInterceptedScoped<TInterface, TImpl>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TInterface : class
        where TImpl : class, TInterface
    {
        services.TryAddScoped<TImpl>();

        if (!configuration.GetValue<bool>(InterceptionToggleKey))
        {
            services.AddScoped<TInterface>(sp => sp.GetRequiredService<TImpl>());
            return services;
        }

        services.AddScoped<TInterface>(sp =>
        {
            var target = sp.GetRequiredService<TImpl>();
            var generator = sp.GetRequiredService<ProxyGenerator>();
            var interceptor = sp.GetRequiredService<LoggingInterceptor>();
            return generator.CreateInterfaceProxyWithTargetInterface<TInterface>(
                target,
                interceptor.ToInterceptor());
        });

        return services;
    }
}
