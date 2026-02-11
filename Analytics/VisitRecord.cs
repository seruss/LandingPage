namespace LandingPage.Analytics;

public class VisitRecord
{
    // SurrealDB auto-generates Id if using Thing type
    public string? Id { get; set; }

    /// <summary>Unique visit identifier (cookie-based, links server + client data)</summary>
    public string VisitId { get; set; } = string.Empty;

    // --- Server-side collected ---
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
    public string? AcceptLanguage { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public int StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // --- Client-side enrichment (filled via JS tracker) ---
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

    // --- Browser fingerprint ---
    public string? FingerprintHash { get; set; }

    // --- Performance metrics (Navigation Timing API) ---
    public double? PageLoadTimeMs { get; set; }
    public double? DomContentLoadedMs { get; set; }
    public double? FirstPaintMs { get; set; }
    public double? FirstContentfulPaintMs { get; set; }
    public double? TimeToInteractiveMs { get; set; }
}
