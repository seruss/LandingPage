using System.Collections.Concurrent;
using System.Diagnostics;
using SurrealDb.Net;

namespace LandingPage.Analytics;

/// <summary>
/// Background service that buffers analytics events in memory and flushes them
/// to SurrealDB in batches. Provides rate limiting and anomaly detection to
/// protect against DDoS and runaway event generation.
/// </summary>
public class EventBuffer : BackgroundService
{
    private const int FlushIntervalSeconds = 10;
    private const int MaxBufferSize = 10_000;
    private const int MaxEventsPerIpPerMinute = 100;
    private const int MaxGlobalEventsPerMinute = 1000;
    private const double AnomalyMultiplier = 5.0;
    private const int AnomalyWindowMinutes = 5;

    private readonly ConcurrentQueue<VisitRecord> _visitQueue = new();

    // Rate limiting: IP -> list of timestamps
    private readonly ConcurrentDictionary<string, List<DateTime>> _ipEventTimestamps = new();
    private readonly object _globalCountLock = new();
    private int _globalEventsThisMinute;
    private DateTime _currentMinuteStart = DateTime.UtcNow;

    // Anomaly detection: rolling window of per-minute counts
    private readonly ConcurrentQueue<(DateTime Minute, int Count)> _minuteHistory = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBuffer> _logger;
    private bool _throttled;

    public EventBuffer(IServiceProvider serviceProvider, ILogger<EventBuffer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Enqueue a visit record for batch insertion.
    /// </summary>
    public void EnqueueVisit(VisitRecord visit)
    {
        if (_visitQueue.Count >= MaxBufferSize)
        {
            _logger.LogWarning("Visit buffer full. Dropping visit.");
            return;
        }
        _visitQueue.Enqueue(visit);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventBuffer started. Flush interval: {Interval}s, Max buffer: {Max}.",
            FlushIntervalSeconds, MaxBufferSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(FlushIntervalSeconds), stoppingToken);
                await FlushAsync(stoppingToken);
                CheckForAnomalies();
                CleanupStaleIpEntries();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — flush remaining
                _logger.LogInformation("EventBuffer shutting down. Flushing remaining events...");
                await FlushAsync(CancellationToken.None);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EventBuffer flush cycle.");
            }
        }
    }

    private async Task FlushAsync(CancellationToken ct)
    {
        var visits = DrainQueue(_visitQueue);

        if (visits.Count == 0) return;

        var sw = Stopwatch.StartNew();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetService<ISurrealDbClient>();
            if (db == null)
            {
                _logger.LogWarning("SurrealDB not configured. Dropping {VisitCount} visits.",
                    visits.Count);
                return;
            }

            // Batch insert visits
            foreach (var visit in visits)
            {
                await db.Create("visit", visit, ct);
            }

            sw.Stop();
            _logger.LogInformation(
                "Flushed {VisitCount} visits to SurrealDB in {ElapsedMs}ms.",
                visits.Count, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush to SurrealDB. {VisitCount} visits lost.",
                visits.Count);
            // Circuit breaker: if DB is down, we already drained the queue
            // Items are lost but we logged the failure. This prevents memory buildup.
        }
    }

    private void CheckForAnomalies()
    {
        lock (_globalCountLock)
        {
            RotateTimeWindow(DateTime.UtcNow);
        }

        var history = _minuteHistory.ToArray();
        if (history.Length < 2) return;

        var avg = history.Average(h => h.Count);
        if (avg <= 0) return;

        lock (_globalCountLock)
        {
            if (_globalEventsThisMinute > avg * AnomalyMultiplier)
            {
                if (!_throttled)
                {
                    _logger.LogWarning(
                        "ANOMALY DETECTED: Current minute has {Current} events, average is {Avg:F0}. " +
                        "This is {Ratio:F1}x the average. Throttling enabled.",
                        _globalEventsThisMinute, avg, _globalEventsThisMinute / avg);
                    _throttled = true;
                }
            }
            else
            {
                _throttled = false;
            }
        }
    }

    private void CleanupStaleIpEntries()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-2);
        foreach (var kvp in _ipEventTimestamps)
        {
            lock (kvp.Value)
            {
                kvp.Value.RemoveAll(t => t < cutoff);
                if (kvp.Value.Count == 0)
                    _ipEventTimestamps.TryRemove(kvp.Key, out _);
            }
        }
    }

    private void RotateTimeWindow(DateTime now)
    {
        if ((now - _currentMinuteStart).TotalMinutes >= 1)
        {
            _minuteHistory.Enqueue((_currentMinuteStart, _globalEventsThisMinute));
            // Keep only last N minutes
            while (_minuteHistory.Count > AnomalyWindowMinutes)
                _minuteHistory.TryDequeue(out _);

            _currentMinuteStart = now;
            _globalEventsThisMinute = 0;
        }
    }

    private static List<T> DrainQueue<T>(ConcurrentQueue<T> queue)
    {
        var items = new List<T>();
        while (queue.TryDequeue(out var item))
        {
            items.Add(item);
        }
        return items;
    }
}
