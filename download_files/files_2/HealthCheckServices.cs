using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using StackExchange.Redis;
using RabbitMQ.Client;

namespace SharedKernel.Infrastructure.Observability.HealthChecks;

/// <summary>
/// Health check for PostgreSQL database connectivity
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public DatabaseHealthCheck(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Execute simple query to verify database is responsive
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            var version = connection.PostgreSqlVersion;
            
            return HealthCheckResult.Healthy($"PostgreSQL is healthy (Version: {version})");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "PostgreSQL is unhealthy",
                exception: ex);
        }
    }
}

/// <summary>
/// Health check for Redis cache connectivity
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _redis.GetDatabase();
            
            // Ping Redis to check connectivity
            var pingResult = await database.PingAsync();
            
            if (pingResult.TotalMilliseconds > 1000)
            {
                return HealthCheckResult.Degraded(
                    $"Redis is responding slowly ({pingResult.TotalMilliseconds}ms)");
            }

            // Get server info
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());
            var info = server.Info("Server");
            var redisVersion = info.FirstOrDefault(x => x.Key == "redis_version")?.FirstOrDefault().Value;

            return HealthCheckResult.Healthy(
                $"Redis is healthy (Version: {redisVersion}, Latency: {pingResult.TotalMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Redis is unhealthy",
                exception: ex);
        }
    }
}

/// <summary>
/// Health check for RabbitMQ message broker connectivity
/// </summary>
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public RabbitMqHealthCheck(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_connectionString)
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Verify connection is open
            if (!connection.IsOpen)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ connection is not open"));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"RabbitMQ is healthy (Server: {connection.Endpoint})"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "RabbitMQ is unhealthy",
                exception: ex));
        }
    }
}

/// <summary>
/// Health check for file system access
/// </summary>
public class FileSystemHealthCheck : IHealthCheck
{
    private readonly string _path;

    public FileSystemHealthCheck(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if directory exists
            if (!Directory.Exists(_path))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Directory does not exist: {_path}"));
            }

            // Try to write a test file
            var testFile = Path.Combine(_path, $"healthcheck_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "health check");
            
            // Try to read the test file
            var content = File.ReadAllText(testFile);
            
            // Clean up
            File.Delete(testFile);

            if (content != "health check")
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"File system read/write verification failed: {_path}"));
            }

            // Check available space
            var drive = new DriveInfo(Path.GetPathRoot(_path)!);
            var availableSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);

            if (availableSpaceGB < 1.0)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Low disk space: {availableSpaceGB:F2} GB available on {drive.Name}"));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"File system is healthy (Available: {availableSpaceGB:F2} GB)"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"File system is unhealthy: {_path}",
                exception: ex));
        }
    }
}

/// <summary>
/// Health check for external HTTP APIs
/// </summary>
public class ExternalApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _url;
    private readonly string _clientName;

    public ExternalApiHealthCheck(IHttpClientFactory httpClientFactory, string url, string clientName = "")
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _url = url ?? throw new ArgumentNullException(nameof(url));
        _clientName = clientName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = string.IsNullOrWhiteSpace(_clientName) 
                ? _httpClientFactory.CreateClient() 
                : _httpClientFactory.CreateClient(_clientName);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.GetAsync(_url, cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Unhealthy(
                    $"External API returned {response.StatusCode}: {_url}");
            }

            if (stopwatch.ElapsedMilliseconds > 5000)
            {
                return HealthCheckResult.Degraded(
                    $"External API is responding slowly ({stopwatch.ElapsedMilliseconds}ms): {_url}");
            }

            return HealthCheckResult.Healthy(
                $"External API is healthy ({stopwatch.ElapsedMilliseconds}ms): {_url}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"External API is unhealthy: {_url}",
                exception: ex);
        }
    }
}

/// <summary>
/// Extension methods for registering health checks
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds PostgreSQL database health check
    /// </summary>
    public static IHealthChecksBuilder AddDatabaseHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string name = "database",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new DatabaseHealthCheck(connectionString),
            failureStatus,
            tags));
    }

    /// <summary>
    /// Adds Redis cache health check
    /// </summary>
    public static IHealthChecksBuilder AddRedisHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "redis",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new RedisHealthCheck(sp.GetRequiredService<IConnectionMultiplexer>()),
            failureStatus,
            tags));
    }

    /// <summary>
    /// Adds RabbitMQ message broker health check
    /// </summary>
    public static IHealthChecksBuilder AddRabbitMqHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string name = "rabbitmq",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new RabbitMqHealthCheck(connectionString),
            failureStatus,
            tags));
    }

    /// <summary>
    /// Adds file system health check
    /// </summary>
    public static IHealthChecksBuilder AddFileSystemHealthCheck(
        this IHealthChecksBuilder builder,
        string path,
        string name = "filesystem",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new FileSystemHealthCheck(path),
            failureStatus,
            tags));
    }

    /// <summary>
    /// Adds external API health check
    /// </summary>
    public static IHealthChecksBuilder AddExternalApiHealthCheck(
        this IHealthChecksBuilder builder,
        string url,
        string name,
        string? clientName = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new ExternalApiHealthCheck(sp.GetRequiredService<IHttpClientFactory>(), url, clientName ?? ""),
            failureStatus,
            tags));
    }
}
