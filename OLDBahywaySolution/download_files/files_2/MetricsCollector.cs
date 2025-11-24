using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel.Infrastructure.Observability.Metrics;

/// <summary>
/// Central metrics collector for BahyWay applications
/// Provides standard metrics and custom metric tracking
/// </summary>
public class MetricsCollector
{
    private readonly Meter _meter;
    
    // Counters
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Counter<long> _backgroundJobCounter;
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _cacheMissCounter;
    private readonly Counter<long> _eventPublishedCounter;
    private readonly Counter<long> _eventConsumedCounter;
    
    // Histograms
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _databaseQueryDuration;
    private readonly Histogram<double> _cacheOperationDuration;
    private readonly Histogram<double> _backgroundJobDuration;
    private readonly Histogram<double> _externalApiDuration;
    
    // Gauges (using ObservableGauge)
    private long _activeRequests;
    private long _queuedBackgroundJobs;

    public MetricsCollector(string applicationName)
    {
        _meter = new Meter($"BahyWay.{applicationName}", "1.0.0");

        // Initialize counters
        _requestCounter = _meter.CreateCounter<long>(
            "http.requests.total",
            unit: "requests",
            description: "Total number of HTTP requests");

        _errorCounter = _meter.CreateCounter<long>(
            "http.errors.total",
            unit: "errors",
            description: "Total number of HTTP errors");

        _backgroundJobCounter = _meter.CreateCounter<long>(
            "background.jobs.total",
            unit: "jobs",
            description: "Total number of background jobs executed");

        _cacheHitCounter = _meter.CreateCounter<long>(
            "cache.hits.total",
            unit: "hits",
            description: "Total number of cache hits");

        _cacheMissCounter = _meter.CreateCounter<long>(
            "cache.misses.total",
            unit: "misses",
            description: "Total number of cache misses");

        _eventPublishedCounter = _meter.CreateCounter<long>(
            "events.published.total",
            unit: "events",
            description: "Total number of events published");

        _eventConsumedCounter = _meter.CreateCounter<long>(
            "events.consumed.total",
            unit: "events",
            description: "Total number of events consumed");

        // Initialize histograms
        _requestDuration = _meter.CreateHistogram<double>(
            "http.request.duration",
            unit: "ms",
            description: "Duration of HTTP requests");

        _databaseQueryDuration = _meter.CreateHistogram<double>(
            "database.query.duration",
            unit: "ms",
            description: "Duration of database queries");

        _cacheOperationDuration = _meter.CreateHistogram<double>(
            "cache.operation.duration",
            unit: "ms",
            description: "Duration of cache operations");

        _backgroundJobDuration = _meter.CreateHistogram<double>(
            "background.job.duration",
            unit: "ms",
            description: "Duration of background job execution");

        _externalApiDuration = _meter.CreateHistogram<double>(
            "external.api.duration",
            unit: "ms",
            description: "Duration of external API calls");

        // Initialize gauges
        _meter.CreateObservableGauge(
            "http.requests.active",
            () => _activeRequests,
            unit: "requests",
            description: "Number of active HTTP requests");

        _meter.CreateObservableGauge(
            "background.jobs.queued",
            () => _queuedBackgroundJobs,
            unit: "jobs",
            description: "Number of queued background jobs");
    }

    #region HTTP Metrics

    public void RecordRequest(string method, string path, int statusCode, double durationMs)
    {
        _requestCounter.Add(1, 
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("path", path),
            new KeyValuePair<string, object?>("status", statusCode));

        _requestDuration.Record(durationMs,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("path", path),
            new KeyValuePair<string, object?>("status", statusCode));

        if (statusCode >= 400)
        {
            _errorCounter.Add(1,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", path),
                new KeyValuePair<string, object?>("status", statusCode));
        }
    }

    public void IncrementActiveRequests() => Interlocked.Increment(ref _activeRequests);
    public void DecrementActiveRequests() => Interlocked.Decrement(ref _activeRequests);

    #endregion

    #region Database Metrics

    public void RecordDatabaseQuery(string operation, string table, double durationMs, bool success)
    {
        _databaseQueryDuration.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("table", table),
            new KeyValuePair<string, object?>("success", success));
    }

    #endregion

    #region Cache Metrics

    public void RecordCacheHit(string key)
    {
        _cacheHitCounter.Add(1,
            new KeyValuePair<string, object?>("key", key));
    }

    public void RecordCacheMiss(string key)
    {
        _cacheMissCounter.Add(1,
            new KeyValuePair<string, object?>("key", key));
    }

    public void RecordCacheOperation(string operation, double durationMs, bool success)
    {
        _cacheOperationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("success", success));
    }

    #endregion

    #region Background Job Metrics

    public void RecordBackgroundJob(string jobName, double durationMs, bool success)
    {
        _backgroundJobCounter.Add(1,
            new KeyValuePair<string, object?>("job", jobName),
            new KeyValuePair<string, object?>("success", success));

        _backgroundJobDuration.Record(durationMs,
            new KeyValuePair<string, object?>("job", jobName),
            new KeyValuePair<string, object?>("success", success));
    }

    public void SetQueuedJobCount(long count) => Interlocked.Exchange(ref _queuedBackgroundJobs, count);

    #endregion

    #region Event Bus Metrics

    public void RecordEventPublished(string eventType)
    {
        _eventPublishedCounter.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType));
    }

    public void RecordEventConsumed(string eventType, bool success)
    {
        _eventConsumedCounter.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("success", success));
    }

    #endregion

    #region External API Metrics

    public void RecordExternalApiCall(string apiName, string endpoint, double durationMs, bool success)
    {
        _externalApiDuration.Record(durationMs,
            new KeyValuePair<string, object?>("api", apiName),
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("success", success));
    }

    #endregion
}

/// <summary>
/// Middleware for automatic HTTP metrics collection
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MetricsCollector _metrics;

    public MetricsMiddleware(RequestDelegate next, MetricsCollector metrics)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip metrics endpoints
        if (context.Request.Path.StartsWithSegments("/metrics"))
        {
            await _next(context);
            return;
        }

        _metrics.IncrementActiveRequests();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.DecrementActiveRequests();
            
            _metrics.RecordRequest(
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}

/// <summary>
/// Business metrics specific to BahyWay applications
/// </summary>
public class BusinessMetrics
{
    private readonly Meter _meter;
    private readonly string _applicationName;

    public BusinessMetrics(string applicationName)
    {
        _applicationName = applicationName;
        _meter = new Meter($"BahyWay.{applicationName}.Business", "1.0.0");
    }

    /// <summary>
    /// Creates a counter for business events
    /// </summary>
    public Counter<long> CreateBusinessCounter(string name, string description, string unit = "count")
    {
        return _meter.CreateCounter<long>(
            $"business.{name}",
            unit: unit,
            description: description);
    }

    /// <summary>
    /// Creates a histogram for business metrics
    /// </summary>
    public Histogram<double> CreateBusinessHistogram(string name, string description, string unit = "units")
    {
        return _meter.CreateHistogram<double>(
            $"business.{name}",
            unit: unit,
            description: description);
    }
}

/// <summary>
/// Extension methods for metrics setup
/// </summary>
public static class MetricsExtensions
{
    /// <summary>
    /// Adds metrics collection to the application
    /// </summary>
    public static IServiceCollection AddBahyWayMetrics(
        this IServiceCollection services,
        string applicationName)
    {
        services.AddSingleton(new MetricsCollector(applicationName));
        services.AddSingleton(new BusinessMetrics(applicationName));
        
        return services;
    }

    /// <summary>
    /// Uses metrics middleware
    /// </summary>
    public static IApplicationBuilder UseMetrics(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MetricsMiddleware>();
    }
}

/// <summary>
/// Example business metrics for specific BahyWay applications
/// </summary>
public static class BahyWayBusinessMetrics
{
    /// <summary>
    /// AlarmInsight specific metrics
    /// </summary>
    public class AlarmInsightMetrics
    {
        private readonly BusinessMetrics _businessMetrics;
        private readonly Counter<long> _alarmsProcessed;
        private readonly Counter<long> _alarmsEscalated;
        private readonly Histogram<double> _alarmPriority;

        public AlarmInsightMetrics(BusinessMetrics businessMetrics)
        {
            _businessMetrics = businessMetrics;
            
            _alarmsProcessed = businessMetrics.CreateBusinessCounter(
                "alarms.processed",
                "Total number of alarms processed");
            
            _alarmsEscalated = businessMetrics.CreateBusinessCounter(
                "alarms.escalated",
                "Total number of alarms escalated");
            
            _alarmPriority = businessMetrics.CreateBusinessHistogram(
                "alarm.priority",
                "Distribution of alarm priorities",
                "priority");
        }

        public void RecordAlarmProcessed(string severity, string location)
        {
            _alarmsProcessed.Add(1,
                new KeyValuePair<string, object?>("severity", severity),
                new KeyValuePair<string, object?>("location", location));
        }

        public void RecordAlarmEscalated(string reason)
        {
            _alarmsEscalated.Add(1,
                new KeyValuePair<string, object?>("reason", reason));
        }
    }

    /// <summary>
    /// ETLway specific metrics
    /// </summary>
    public class ETLwayMetrics
    {
        private readonly BusinessMetrics _businessMetrics;
        private readonly Counter<long> _filesProcessed;
        private readonly Histogram<double> _fileSize;
        private readonly Histogram<double> _recordsProcessed;

        public ETLwayMetrics(BusinessMetrics businessMetrics)
        {
            _businessMetrics = businessMetrics;
            
            _filesProcessed = businessMetrics.CreateBusinessCounter(
                "files.processed",
                "Total number of files processed");
            
            _fileSize = businessMetrics.CreateBusinessHistogram(
                "file.size",
                "Size of processed files",
                "bytes");
            
            _recordsProcessed = businessMetrics.CreateBusinessHistogram(
                "records.processed",
                "Number of records processed per file",
                "records");
        }

        public void RecordFileProcessed(string fileType, long fileSize, long recordCount, bool success)
        {
            _filesProcessed.Add(1,
                new KeyValuePair<string, object?>("file_type", fileType),
                new KeyValuePair<string, object?>("success", success));
            
            _fileSize.Record(fileSize,
                new KeyValuePair<string, object?>("file_type", fileType));
            
            _recordsProcessed.Record(recordCount,
                new KeyValuePair<string, object?>("file_type", fileType));
        }
    }
}
