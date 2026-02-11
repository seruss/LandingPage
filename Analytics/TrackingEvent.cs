namespace LandingPage.Analytics;

public class TrackingEvent
{
    public string? Id { get; set; }

    /// <summary>Links to VisitRecord.VisitId</summary>
    public string VisitId { get; set; } = string.Empty;

    /// <summary>Event type: page_view, click, scroll, section_view, mouse_move, visibility_change, unload</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>JSON payload with event-specific data</summary>
    public string? EventData { get; set; }

    /// <summary>Page URL where the event occurred</summary>
    public string? PageUrl { get; set; }

    /// <summary>Client-side timestamp (UTC)</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
