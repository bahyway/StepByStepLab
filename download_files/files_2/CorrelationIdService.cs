namespace BahyWay.SharedKernel.Application.Abstractions;

/// <summary>
/// Manages correlation IDs for tracking requests across services and operations.
/// Essential for distributed tracing and troubleshooting.
/// </summary>
public interface ICorrelationIdService
{
    /// <summary>
    /// Gets the current correlation ID for this request/operation.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Sets the correlation ID for the current context.
    /// </summary>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Generates a new correlation ID.
    /// </summary>
    string GenerateCorrelationId();
}

/// <summary>
/// Default implementation using AsyncLocal for correlation ID storage.
/// Thread-safe and async-safe.
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private static readonly AsyncLocal<string> _correlationId = new();

    public string CorrelationId => _correlationId.Value ?? GenerateCorrelationId();

    public void SetCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));

        _correlationId.Value = correlationId;
    }

    public string GenerateCorrelationId()
    {
        var id = Guid.NewGuid().ToString("N");
        _correlationId.Value = id;
        return id;
    }
}
