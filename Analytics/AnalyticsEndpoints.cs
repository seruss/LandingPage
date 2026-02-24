using System.Text.Json;
using SurrealDb.Net;

namespace LandingPage.Analytics;

/// <summary>
/// Minimal API endpoints for receiving client-side analytics data.
/// </summary>
public static class AnalyticsEndpoints
{
    private const int MaxPayloadBytes = 50 * 1024; // 50KB max

    public const string BasePath = "/api/t";
    public const string EnrichPath = "/e";
    
    public const string FullEnrichPath = BasePath + EnrichPath;

    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(BasePath);

        // Client enrichment: screen info, fingerprint, performance data
        group.MapPost(EnrichPath, async (HttpContext context) =>
        {
            var db = context.RequestServices.GetService<ISurrealDbClient>();
            if (db == null)
                return Results.StatusCode(503); // SurrealDB not configured

            if (context.Request.ContentLength > MaxPayloadBytes)
                return Results.StatusCode(413);

            var body = await JsonSerializer.DeserializeAsync<EnrichPayload>(
                context.Request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (body == null || string.IsNullOrWhiteSpace(body.VisitId))
                return Results.BadRequest();

            try
            {
                // Find existing visit and update with client data
                var query = await db.RawQuery(
                    "UPDATE visit SET " +
                    "ScreenWidth = $screenWidth, ScreenHeight = $screenHeight, " +
                    "ViewportWidth = $viewportWidth, ViewportHeight = $viewportHeight, " +
                    "DevicePixelRatio = $devicePixelRatio, Platform = $platform, " +
                    "Language = $language, CookiesEnabled = $cookiesEnabled, " +
                    "DoNotTrack = $doNotTrack, ConnectionType = $connectionType, " +
                    "HardwareConcurrency = $hardwareConcurrency, DeviceMemory = $deviceMemory, " +
                    "TouchSupport = $touchSupport, ColorDepth = $colorDepth, " +
                    "TimezoneOffset = $timezoneOffset, Timezone = $timezone, " +
                    "FingerprintHash = $fingerprintHash, " +
                    "PageLoadTimeMs = $pageLoadTimeMs, DomContentLoadedMs = $domContentLoadedMs, " +
                    "FirstPaintMs = $firstPaintMs, FirstContentfulPaintMs = $firstContentfulPaintMs, " +
                    "TimeToInteractiveMs = $timeToInteractiveMs " +
                    "WHERE VisitId = $visitId",
                    new Dictionary<string, object?>
                    {
                        ["visitId"] = body.VisitId,
                        ["screenWidth"] = body.ScreenWidth,
                        ["screenHeight"] = body.ScreenHeight,
                        ["viewportWidth"] = body.ViewportWidth,
                        ["viewportHeight"] = body.ViewportHeight,
                        ["devicePixelRatio"] = body.DevicePixelRatio,
                        ["platform"] = body.Platform,
                        ["language"] = body.Language,
                        ["cookiesEnabled"] = body.CookiesEnabled,
                        ["doNotTrack"] = body.DoNotTrack,
                        ["connectionType"] = body.ConnectionType,
                        ["hardwareConcurrency"] = body.HardwareConcurrency,
                        ["deviceMemory"] = body.DeviceMemory,
                        ["touchSupport"] = body.TouchSupport,
                        ["colorDepth"] = body.ColorDepth,
                        ["timezoneOffset"] = body.TimezoneOffset,
                        ["timezone"] = body.Timezone,
                        ["fingerprintHash"] = body.FingerprintHash,
                        ["pageLoadTimeMs"] = body.PageLoadTimeMs,
                        ["domContentLoadedMs"] = body.DomContentLoadedMs,
                        ["firstPaintMs"] = body.FirstPaintMs,
                        ["firstContentfulPaintMs"] = body.FirstContentfulPaintMs,
                        ["timeToInteractiveMs"] = body.TimeToInteractiveMs,
                    });

                return Results.Ok();
            }
            catch (Exception)
            {
                return Results.StatusCode(500);
            }
        });
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

// --- Request DTOs ---

public class EnrichPayload
{
    public string? VisitId { get; set; }
    public int? ScreenWidth { get; set; }
    public int? ScreenHeight { get; set; }
    public int? ViewportWidth { get; set; }
    public int? ViewportHeight { get; set; }
    public double? DevicePixelRatio { get; set; }
    public string? Platform { get; set; }
    public string? Language { get; set; }
    public bool? CookiesEnabled { get; set; }
    public bool? DoNotTrack { get; set; }
    public string? ConnectionType { get; set; }
    public int? HardwareConcurrency { get; set; }
    public double? DeviceMemory { get; set; }
    public bool? TouchSupport { get; set; }
    public int? ColorDepth { get; set; }
    public int? TimezoneOffset { get; set; }
    public string? Timezone { get; set; }
    public string? FingerprintHash { get; set; }
    public double? PageLoadTimeMs { get; set; }
    public double? DomContentLoadedMs { get; set; }
    public double? FirstPaintMs { get; set; }
    public double? FirstContentfulPaintMs { get; set; }
    public double? TimeToInteractiveMs { get; set; }
}
