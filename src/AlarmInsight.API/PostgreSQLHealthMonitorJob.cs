using BahyWay.SharedKernel.Application.Abstractions;

namespace AlarmInsight.API;

/// <summary>
/// Hangfire background job that monitors PostgreSQL cluster health
/// and logs any issues or alarms
/// </summary>
public class PostgreSQLHealthMonitorJob
{
    private readonly IPostgreSQLHealthService _healthService;
    private readonly ILogger<PostgreSQLHealthMonitorJob> _logger;

    public PostgreSQLHealthMonitorJob(
        IPostgreSQLHealthService healthService,
        ILogger<PostgreSQLHealthMonitorJob> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    /// <summary>
    /// Monitors PostgreSQL cluster health and reports any issues
    /// This method is called by Hangfire on a recurring schedule
    /// </summary>
    public async Task MonitorHealthAsync()
    {
        try
        {
            _logger.LogInformation("Starting PostgreSQL health monitoring check");

            // Get comprehensive cluster health
            var health = await _healthService.GetClusterHealthAsync(
                includeHAProxy: false,
                includeBarman: false
            );

            var isHealthy = health.TryGetValue("IsHealthy", out var healthStatus)
                && healthStatus is bool healthy
                && healthy;

            if (!isHealthy)
            {
                _logger.LogWarning("PostgreSQL cluster is unhealthy");

                // Get all issues
                if (health.TryGetValue("AllIssues", out var issues))
                {
                    _logger.LogWarning("Issues detected: {Issues}", issues);
                }

                // Check for alarms
                var alarms = await _healthService.GetHealthAlarmsAsync();
                if (alarms.Any())
                {
                    _logger.LogWarning("Active alarms count: {AlarmCount}", alarms.Count);
                    foreach (var alarm in alarms)
                    {
                        if (alarm.TryGetValue("Message", out var message))
                        {
                            _logger.LogWarning("Alarm: {Message}", message);
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation("PostgreSQL cluster health check passed");
            }

            // Check replication lag
            var replicationLag = await _healthService.GetReplicationLagAsync();
            if (replicationLag.TryGetValue("LagSeconds", out var lagSeconds)
                && lagSeconds is int lag && lag > 10)
            {
                _logger.LogWarning(
                    "High replication lag detected: {LagSeconds} seconds",
                    lag
                );
            }

            // Check connection count
            var connections = await _healthService.GetConnectionCountAsync();
            if (connections.TryGetValue("TotalConnections", out var totalConnections))
            {
                _logger.LogInformation(
                    "Current connection count: {ConnectionCount}",
                    totalConnections
                );
            }

            _logger.LogInformation("PostgreSQL health monitoring check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during PostgreSQL health monitoring"
            );
            throw; // Re-throw to let Hangfire handle retries
        }
    }

    /// <summary>
    /// Performs detailed replication monitoring
    /// Can be scheduled separately for more frequent checks
    /// </summary>
    public async Task MonitorReplicationAsync()
    {
        try
        {
            _logger.LogInformation("Starting replication monitoring");

            var replicationStatus = await _healthService.TestReplicationStatusAsync();

            var isReplicating = replicationStatus.TryGetValue("IsReplicating", out var status)
                && status is bool replicating
                && replicating;

            if (!isReplicating)
            {
                _logger.LogError("Replication is not active!");
            }

            var lag = await _healthService.GetReplicationLagAsync();
            if (lag.TryGetValue("LagBytes", out var lagBytes)
                && lagBytes is long bytes)
            {
                _logger.LogInformation("Replication lag: {LagBytes} bytes", bytes);
            }

            _logger.LogInformation("Replication monitoring completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during replication monitoring");
            throw;
        }
    }
}
