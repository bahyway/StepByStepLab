using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BahyWay.SharedKernel.Infrastructure.HealthChecks;

/// <summary>
/// Base class for custom health checks in BahyWay applications.
/// </summary>
public abstract class BaseHealthCheck : IHealthCheck
{
    protected abstract string ComponentName { get; }

    public abstract Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default);

    protected HealthCheckResult Healthy(string description = null, Dictionary<string, object> data = null)
    {
        return HealthCheckResult.Healthy(
            description ?? $"{ComponentName} is healthy",
            data);
    }

    protected HealthCheckResult Degraded(string description, Dictionary<string, object> data = null)
    {
        return HealthCheckResult.Degraded(
            description ?? $"{ComponentName} is degraded",
            data: data);
    }

    protected HealthCheckResult Unhealthy(string description, Exception exception = null, Dictionary<string, object> data = null)
    {
        return HealthCheckResult.Unhealthy(
            description ?? $"{ComponentName} is unhealthy",
            exception,
            data);
    }
}

/// <summary>
/// Health check for PostgreSQL database connectivity.
/// </summary>
public class DatabaseHealthCheck : BaseHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;
    protected override string ComponentName => "PostgreSQL Database";

    public DatabaseHealthCheck(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public override async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            await connection.OpenAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["database"] = connection.Database,
                ["server"] = connection.DataSource
            };

            return Healthy("Database connection successful", data);
        }
        catch (Exception ex)
        {
            return Unhealthy("Database connection failed", ex);
        }
    }
}

/// <summary>
/// Health check for Redis cache connectivity.
/// </summary>
public class RedisHealthCheck : BaseHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    protected override string ComponentName => "Redis Cache";

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public override async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _redis.GetDatabase();
            await database.PingAsync();

            var data = new Dictionary<string, object>
            {
                ["connected_endpoints"] = _redis.GetEndPoints().Length,
                ["is_connected"] = _redis.IsConnected
            };

            return Healthy("Redis is responsive", data);
        }
        catch (Exception ex)
        {
            return Unhealthy("Redis connection failed", ex);
        }
    }
}

/// <summary>
/// Health check for file system access.
/// </summary>
public class FileSystemHealthCheck : BaseHealthCheck
{
    private readonly string _path;
    protected override string ComponentName => "File System";

    public FileSystemHealthCheck(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public override Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(_path);
            
            if (!directoryInfo.Exists)
            {
                return Task.FromResult(Unhealthy($"Directory does not exist: {_path}"));
            }

            // Check read access
            _ = directoryInfo.GetFiles();

            // Check write access
            var testFile = Path.Combine(_path, $".health_check_{Guid.NewGuid():N}");
            File.WriteAllText(testFile, "health check");
            File.Delete(testFile);

            var data = new Dictionary<string, object>
            {
                ["path"] = _path,
                ["readable"] = true,
                ["writable"] = true
            };

            return Task.FromResult(Healthy("File system is accessible", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Unhealthy("File system access failed", ex));
        }
    }
}

// Interface placeholder - would need actual implementation
public interface IDbConnectionFactory
{
    Task<System.Data.IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken);
}

// Interface placeholder - would need StackExchange.Redis
public interface IConnectionMultiplexer
{
    bool IsConnected { get; }
    System.Net.EndPoint[] GetEndPoints();
    IDatabase GetDatabase();
}

public interface IDatabase
{
    Task<TimeSpan> PingAsync();
}
