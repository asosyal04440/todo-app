using TodoApi.API.BackgroundJobs;

namespace TodoApi.API.Security;

public sealed class SecurityHeadersMiddleware
{
    private const string DashboardContentSecurityPolicy =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        bool isDashboardRequest = IsDashboardRequest(httpContext);

        httpContext.Response.OnStarting(() =>
        {
            IHeaderDictionary headers = httpContext.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";

            headers["X-Frame-Options"] = "DENY";

            headers["Content-Security-Policy"] = isDashboardRequest
                ? DashboardContentSecurityPolicy
                : "default-src 'none'; frame-ancestors 'none'";

            headers["Referrer-Policy"] = "no-referrer";

            headers["Cache-Control"] = "no-store";

            return Task.CompletedTask;
        });

        await _next(httpContext);
    }

    private static bool IsDashboardRequest(HttpContext httpContext)
    {
        return httpContext.Request.Path.StartsWithSegments(BackgroundJobsSetup.DashboardPath);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
