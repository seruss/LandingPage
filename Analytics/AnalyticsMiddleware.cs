using System.Diagnostics;

namespace LandingPage.Analytics;

/// <summary>
/// ASP.NET middleware that captures server-side request data for every
/// non-static-asset page request and enqueues it into the EventBuffer.
/// </summary>
public class AnalyticsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AnalyticsMiddleware> _logger;

    // Extensions to skip tracking (static assets)
    private static readonly HashSet<string> SkipExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".woff", ".woff2",
        ".ttf", ".eot", ".map", ".webp", ".avif", ".mp4", ".webm"
    };

    // Paths to skip entirely
    private static readonly string[] SkipPrefixes = { "/_framework", "/_blazor", "/_content" };

    public AnalyticsMiddleware(RequestDelegate next, ILogger<AnalyticsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, EventBuffer eventBuffer)
    {
        var path = context.Request.Path.Value ?? "/";

        // Skip static assets and framework paths
        if (ShouldSkip(path))
        {
            await _next(context);
            return;
        }

        // Also skip our own analytics endpoints 
        if (path.StartsWith("/api/analytics", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        // Generate or read visit ID cookie
        var visitId = GetOrCreateVisitId(context);

        await _next(context);

        sw.Stop();

        var ip = GetClientIp(context);

        var visit = new VisitRecord
        {
            VisitId = visitId,
            IpAddress = ip,
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            Referer = context.Request.Headers.Referer.ToString(),
            AcceptLanguage = context.Request.Headers.AcceptLanguage.ToString(),
            RequestPath = path,
            HttpMethod = context.Request.Method,
            StatusCode = context.Response.StatusCode,
            ResponseTimeMs = sw.ElapsedMilliseconds,
            Timestamp = DateTime.UtcNow
        };

        eventBuffer.EnqueueVisit(visit);
    }

    private static string GetOrCreateVisitId(HttpContext context)
    {
        const string cookieName = "_vid";

        if (context.Request.Cookies.TryGetValue(cookieName, out var existingId) &&
            !string.IsNullOrWhiteSpace(existingId))
        {
            return existingId;
        }

        var newId = Guid.NewGuid().ToString("N");

        context.Response.Cookies.Append(cookieName, newId, new CookieOptions
        {
            HttpOnly = false, // JS tracker needs to read this
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(365),
            Path = "/"
        });

        return newId;
    }

    private static string GetClientIp(HttpContext context)
    {
        // Check X-Forwarded-For first (reverse proxy / load balancer)
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            // Take the first IP (client IP)
            var firstIp = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return firstIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool ShouldSkip(string path)
    {
        // Skip known framework paths
        foreach (var prefix in SkipPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Skip static asset extensions
        var extension = Path.GetExtension(path);
        if (!string.IsNullOrEmpty(extension) && SkipExtensions.Contains(extension))
            return true;

        return false;
    }
}

public static class AnalyticsMiddlewareExtensions
{
    public static IApplicationBuilder UseAnalytics(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AnalyticsMiddleware>();
    }
}
