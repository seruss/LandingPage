using System.Text.Json;
using SurrealDb.Net;

namespace LandingPage.Analytics;

/// <summary>
/// Minimal API endpoints for receiving client-side analytics data.
/// </summary>
public static class AnalyticsEndpoints
{
    private const int MaxPayloadBytes = 50 * 1024; // 50KB max

    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/analytics");

        // Client enrichment: screen info, fingerprint, performance data
        group.MapPost("/enrich", async (HttpContext context, ISurrealDbClient db) =>
        {
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

        // Batch events: clicks, scrolls, mouse moves, etc.
        group.MapPost("/events", async (HttpContext context, EventBuffer buffer) =>
        {
            if (context.Request.ContentLength > MaxPayloadBytes)
                return Results.StatusCode(413);

            var body = await JsonSerializer.DeserializeAsync<EventBatchPayload>(
                context.Request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (body?.Events == null || body.Events.Count == 0)
                return Results.BadRequest();

            var ip = GetClientIp(context);
            var accepted = 0;

            foreach (var evt in body.Events)
            {
                var trackingEvent = new TrackingEvent
                {
                    VisitId = evt.VisitId ?? body.VisitId ?? string.Empty,
                    EventType = evt.EventType ?? "unknown",
                    EventData = evt.EventData is JsonElement el ? el.GetRawText() : evt.EventData?.ToString(),
                    PageUrl = evt.PageUrl,
                    Timestamp = evt.Timestamp ?? DateTime.UtcNow
                };

                if (buffer.TryEnqueueEvent(trackingEvent, ip))
                    accepted++;
            }

            return Results.Ok(new { accepted, total = body.Events.Count });
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

public class EventBatchPayload
{
    public string? VisitId { get; set; }
    public List<EventPayloadItem> Events { get; set; } = new();
}

public class EventPayloadItem
{
    public string? VisitId { get; set; }
    public string? EventType { get; set; }
    public object? EventData { get; set; }
    public string? PageUrl { get; set; }
    public DateTime? Timestamp { get; set; }
}
