using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace SharedKernel.Infrastructure.Observability.Logging;

/// <summary>
/// Middleware that ensures every request has a correlation ID for distributed tracing
/// Correlation IDs are used to track requests across multiple services
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string CorrelationIdLogPropertyName = "CorrelationId";
    
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Add to response headers so clients can track requests
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers.Add(CorrelationIdHeaderName, correlationId);
            }
            return Task.CompletedTask;
        });

        // Add to HttpContext items for easy access throughout the pipeline
        context.Items[CorrelationIdLogPropertyName] = correlationId;

        // Push to Serilog LogContext so it's included in all logs for this request
        using (LogContext.PushProperty(CorrelationIdLogPropertyName, correlationId))
        {
            _logger.LogDebug("Request started with correlation ID: {CorrelationId}", correlationId);
            
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during request with correlation ID: {CorrelationId}", correlationId);
                throw;
            }
            finally
            {
                _logger.LogDebug("Request completed with correlation ID: {CorrelationId}", correlationId);
            }
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check if correlation ID exists in request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out StringValues correlationId) 
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Create new correlation ID
        return Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Service for accessing correlation ID throughout the application
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// Gets the correlation ID for the current request
    /// </summary>
    string? GetCorrelationId();
}

/// <summary>
/// Implementation of correlation ID accessor
/// </summary>
public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public string? GetCorrelationId()
    {
        return _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
    }
}

/// <summary>
/// Extension methods for correlation ID middleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds correlation ID middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
