using Microsoft.Extensions.Logging;

namespace BahyWay.SharedKernel.Infrastructure.Logging;

/// <summary>
/// Serilog-based implementation of application logging.
/// Wraps Microsoft.Extensions.Logging.ILogger with additional structured logging capabilities.
/// </summary>
public class ApplicationLogger<T> : IApplicationLogger<T>
{
    private readonly ILogger<T> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public ApplicationLogger(ILogger<T> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlationIdService = correlationIdService ?? throw new ArgumentNullException(nameof(correlationIdService));
    }

    public void LogDebug(string message, params object[] args)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = _correlationIdService.CorrelationId
        }))
        {
            _logger.LogDebug(message, args);
        }
    }

    public void LogInformation(string message, params object[] args)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = _correlationIdService.CorrelationId
        }))
        {
            _logger.LogInformation(message, args);
        }
    }

    public void LogWarning(string message, params object[] args)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = _correlationIdService.CorrelationId
        }))
        {
            _logger.LogWarning(message, args);
        }
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = _correlationIdService.CorrelationId,
            ["ExceptionType"] = exception.GetType().Name
        }))
        {
            _logger.LogError(exception, message, args);
        }
    }

    public void LogCritical(Exception exception, string message, params object[] args)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = _correlationIdService.CorrelationId,
            ["ExceptionType"] = exception.GetType().Name,
            ["Severity"] = "CRITICAL"
        }))
        {
            _logger.LogCritical(exception, message, args);
        }
    }

    public IDisposable BeginScope(Dictionary<string, object> properties)
    {
        properties["CorrelationId"] = _correlationIdService.CorrelationId;
        return _logger.BeginScope(properties);
    }

    public void LogInformationWithProperties(string message, Dictionary<string, object> properties)
    {
        properties["CorrelationId"] = _correlationIdService.CorrelationId;
        
        using (_logger.BeginScope(properties))
        {
            _logger.LogInformation(message);
        }
    }
}
