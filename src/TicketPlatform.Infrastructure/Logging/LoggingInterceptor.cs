using System.Security.Claims;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TicketPlatform.Infrastructure.Logging;

public class LoggingInterceptor(
    ILogger<LoggingInterceptor> logger,
    IHttpContextAccessor httpContextAccessor) : AsyncInterceptorBase
{
    protected override async Task InterceptAsync(
        IInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<IInvocation, IInvocationProceedInfo, Task> proceed)
    {
        LogInvocation(invocation);
        await proceed(invocation, proceedInfo);
    }

    protected override async Task<TResult> InterceptAsync<TResult>(
        IInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
    {
        LogInvocation(invocation);
        return await proceed(invocation, proceedInfo);
    }

    private void LogInvocation(IInvocation invocation)
    {
        var user = httpContextAccessor.HttpContext?.User;

        var username = user?.Identity?.IsAuthenticated == true
            ? user.Identity.Name ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "<unnamed>"
            : "anonymous";

        var roles = user?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
                    ?? Array.Empty<string>();
        var rolesText = roles.Length > 0 ? string.Join(",", roles) : "none";

        var className = invocation.TargetType?.Name
                        ?? invocation.Method.DeclaringType?.Name
                        ?? "unknown";
        var methodName = invocation.Method.Name;

        logger.LogInformation(
            "[Audit] {Time:O} user={User} roles=[{Roles}] {Class}.{Method}",
            DateTimeOffset.UtcNow,
            username,
            rolesText,
            className,
            methodName);
    }
}
