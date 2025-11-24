namespace BahyWay.SharedKernel.Application.Abstractions;

/// <summary>
/// Application-level logging abstraction.
/// Provides structured logging with correlation and context support.
/// </summary>
public interface IApplicationLogger<T>
{
    /// <summary>
    /// Log debug information for troubleshooting.
    /// </summary>
    void LogDebug(string message, params object[] args);

    /// <summary>
    /// Log general informational messages.
    /// </summary>
    void LogInformation(string message, params object[] args);

    /// <summary>
    /// Log warning messages for potentially harmful situations.
    /// </summary>
    void LogWarning(string message, params object[] args);

    /// <summary>
    /// Log error messages for failures.
    /// </summary>
    void LogError(Exception exception, string message, params object[] args);

    /// <summary>
    /// Log critical errors that require immediate attention.
    /// </summary>
    void LogCritical(Exception exception, string message, params object[] args);

    /// <summary>
    /// Create a structured logging scope with additional properties.
    /// </summary>
    IDisposable BeginScope(Dictionary<string, object> properties);

    /// <summary>
    /// Log with custom properties for structured logging.
    /// </summary>
    void LogInformationWithProperties(string message, Dictionary<string, object> properties);
}
