using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace SharedKernel.Infrastructure.Observability.Logging;

/// <summary>
/// Middleware that logs detailed information about HTTP requests and responses
/// Includes timing, status codes, and optionally request/response bodies
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly RequestLoggingOptions _options;

    public RequestLoggingMiddleware(
        RequestDelegate next, 
        ILogger<RequestLoggingMiddleware> logger,
        RequestLoggingOptions? options = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new RequestLoggingOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for health checks and metrics endpoints
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestInfo = await CaptureRequestInfo(context);

        // Buffer the response body to capture it for logging
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
            stopwatch.Stop();

            var responseInfo = await CaptureResponseInfo(context, responseBodyStream);

            LogRequest(requestInfo, responseInfo, stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogRequestError(requestInfo, ex, stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            context.Response.Body = originalResponseBodyStream;
        }
    }

    private bool ShouldSkipLogging(PathString path)
    {
        return _options.ExcludedPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<RequestInfo> CaptureRequestInfo(HttpContext context)
    {
        var request = context.Request;
        
        var info = new RequestInfo
        {
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.ToString(),
            Scheme = request.Scheme,
            Host = request.Host.ToString(),
            ContentType = request.ContentType,
            ContentLength = request.ContentLength
        };

        // Capture request body if enabled and not too large
        if (_options.LogRequestBody 
            && request.ContentLength.HasValue 
            && request.ContentLength.Value > 0
            && request.ContentLength.Value <= _options.MaxBodyLength)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            info.Body = await reader.ReadToEndAsync();
            request.Body.Seek(0, SeekOrigin.Begin);
        }

        // Capture headers if enabled
        if (_options.LogHeaders)
        {
            info.Headers = request.Headers
                .Where(h => !_options.SensitiveHeaders.Contains(h.Key))
                .ToDictionary(h => h.Key, h => h.Value.ToString());
        }

        return info;
    }

    private async Task<ResponseInfo> CaptureResponseInfo(HttpContext context, MemoryStream responseBodyStream)
    {
        var response = context.Response;
        
        var info = new ResponseInfo
        {
            StatusCode = response.StatusCode,
            ContentType = response.ContentType,
            ContentLength = responseBodyStream.Length
        };

        // Capture response body if enabled and not too large
        if (_options.LogResponseBody 
            && responseBodyStream.Length > 0
            && responseBodyStream.Length <= _options.MaxBodyLength)
        {
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
            info.Body = await reader.ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);
        }

        // Capture headers if enabled
        if (_options.LogHeaders)
        {
            info.Headers = response.Headers
                .Where(h => !_options.SensitiveHeaders.Contains(h.Key))
                .ToDictionary(h => h.Key, h => h.Value.ToString());
        }

        return info;
    }

    private void LogRequest(RequestInfo request, ResponseInfo response, long elapsedMs)
    {
        var logLevel = DetermineLogLevel(response.StatusCode);

        _logger.Log(logLevel,
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
            request.Method,
            request.Path,
            response.StatusCode,
            elapsedMs);

        // Detailed logging for errors or when explicitly enabled
        if (logLevel >= LogLevel.Warning || _options.LogDetailedInfo)
        {
            _logger.LogInformation(
                "Request Details: {RequestDetails}, Response Details: {ResponseDetails}",
                request,
                response);
        }

        // Performance warning for slow requests
        if (elapsedMs > _options.SlowRequestThresholdMs)
        {
            _logger.LogWarning(
                "Slow request detected: {Method} {Path} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                request.Method,
                request.Path,
                elapsedMs,
                _options.SlowRequestThresholdMs);
        }
    }

    private void LogRequestError(RequestInfo request, Exception exception, long elapsedMs)
    {
        _logger.LogError(exception,
            "HTTP {Method} {Path} failed after {ElapsedMs}ms. Request: {RequestDetails}",
            request.Method,
            request.Path,
            elapsedMs,
            request);
    }

    private LogLevel DetermineLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }

    private class RequestInfo
    {
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string QueryString { get; set; } = string.Empty;
        public string Scheme { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long? ContentLength { get; set; }
        public string? Body { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }

    private class ResponseInfo
    {
        public int StatusCode { get; set; }
        public string? ContentType { get; set; }
        public long ContentLength { get; set; }
        public string? Body { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }
}

/// <summary>
/// Configuration options for request logging
/// </summary>
public class RequestLoggingOptions
{
    /// <summary>
    /// Whether to log request bodies (default: false for security)
    /// </summary>
    public bool LogRequestBody { get; set; } = false;

    /// <summary>
    /// Whether to log response bodies (default: false for performance)
    /// </summary>
    public bool LogResponseBody { get; set; } = false;

    /// <summary>
    /// Whether to log request/response headers (default: true)
    /// </summary>
    public bool LogHeaders { get; set; } = true;

    /// <summary>
    /// Whether to log detailed info for all requests (default: false)
    /// </summary>
    public bool LogDetailedInfo { get; set; } = false;

    /// <summary>
    /// Maximum body size to log in bytes (default: 10KB)
    /// </summary>
    public int MaxBodyLength { get; set; } = 10240;

    /// <summary>
    /// Threshold in milliseconds for slow request warning (default: 5000ms)
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 5000;

    /// <summary>
    /// Paths to exclude from logging (e.g., health checks)
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/healthz",
        "/ready",
        "/metrics",
        "/swagger"
    };

    /// <summary>
    /// Headers that should not be logged (security sensitive)
    /// </summary>
    public HashSet<string> SensitiveHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-API-Key",
        "X-Auth-Token"
    };
}

/// <summary>
/// Extension methods for request logging middleware
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds request logging middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(
        this IApplicationBuilder builder,
        Action<RequestLoggingOptions>? configureOptions = null)
    {
        var options = new RequestLoggingOptions();
        configureOptions?.Invoke(options);

        return builder.UseMiddleware<RequestLoggingMiddleware>(options);
    }
}
