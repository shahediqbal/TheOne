using System.Security.Claims;
using TheOne.Application.Administration;
namespace TheOne.API.Authentication;
/// <summary>Records security request outcomes without reading sensitive payloads.</summary>
public sealed class SecurityRequestAuditMiddleware(RequestDelegate next, ILogger<SecurityRequestAuditMiddleware> logger)
{
    /// <summary>Records completed authentication requests and denied administrative requests.</summary>
    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopes)
    {
        await next(context);
        var path = context.Request.Path.Value ?? "";
        var authentication = path.StartsWith("/api/v1/auth/", StringComparison.Ordinal) && context.Request.Method == "POST";
        var denied = path.StartsWith("/api/v1/admin/", StringComparison.Ordinal) && context.Response.StatusCode is 401 or 403;
        if (!authentication && !denied) return;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ISecurityRequestAudit>().RecordAsync(
                Guid.TryParse(context.User.FindFirstValue("sub"), out var id) ? id : null,
                denied ? "Access.Denied" : "Authentication.Request", path[..Math.Min(path.Length, 200)],
                context.Response.StatusCode, timeout.Token);
        }
        catch (Exception ex)
        {
            // An already-committed login/reset must not be reported as failed because its separate outcome log failed.
            logger.LogError(ex, "Security request audit could not be persisted.");
        }
    }
}
