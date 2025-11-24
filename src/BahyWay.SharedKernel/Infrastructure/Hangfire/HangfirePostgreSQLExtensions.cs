using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BahyWay.SharedKernel.Infrastructure.Hangfire;

/// <summary>
/// Extension methods for configuring Hangfire with PostgreSQL storage and HA support
/// </summary>
public static class HangfirePostgreSQLExtensions
{
    /// <summary>
    /// Adds Hangfire with PostgreSQL storage configured for high availability
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="connectionStringName">Name of the connection string in configuration (default: "HangfireConnection")</param>
    /// <param name="serverName">Name for this Hangfire server instance (default: application name)</param>
    /// <param name="workerCount">Number of worker threads (default: ProcessorCount * 5)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHangfireWithPostgreSQL(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "HangfireConnection",
        string? serverName = null,
        int? workerCount = null)
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' not found in configuration");

        // Configure Hangfire with PostgreSQL
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

        // Add Hangfire server
        services.AddHangfireServer(options =>
        {
            options.ServerName = serverName ?? Environment.MachineName;
            options.WorkerCount = workerCount ?? Environment.ProcessorCount * 5;

            // Configure for HA scenarios
            options.HeartbeatInterval = TimeSpan.FromSeconds(30);
            options.ServerCheckInterval = TimeSpan.FromMinutes(1);
            options.ServerTimeout = TimeSpan.FromMinutes(5);

            // Recommended settings for distributed systems
            options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
        });

        return services;
    }

    /// <summary>
    /// Adds Hangfire with PostgreSQL storage and automatic health monitoring job scheduling
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="scheduleHealthMonitoring">Whether to schedule automatic PostgreSQL health monitoring (default: true)</param>
    /// <param name="healthCheckCron">Cron expression for health checks (default: every 5 minutes)</param>
    /// <param name="connectionStringName">Name of the connection string in configuration</param>
    /// <param name="serverName">Name for this Hangfire server instance</param>
    /// <param name="workerCount">Number of worker threads</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHangfireWithPostgreSQLAndHealthMonitoring(
        this IServiceCollection services,
        IConfiguration configuration,
        bool scheduleHealthMonitoring = true,
        string healthCheckCron = "*/5 * * * *", // Every 5 minutes
        string connectionStringName = "HangfireConnection",
        string? serverName = null,
        int? workerCount = null)
    {
        // Add Hangfire with PostgreSQL
        services.AddHangfireWithPostgreSQL(
            configuration,
            connectionStringName,
            serverName,
            workerCount);

        // Schedule health monitoring if requested
        if (scheduleHealthMonitoring)
        {
            // This will be executed after the application starts
            services.AddHostedService<HangfireHealthMonitoringInitializer>();
        }

        return services;
    }

    /// <summary>
    /// Adds a custom recurring Hangfire job
    /// </summary>
    /// <typeparam name="TJob">The job type</typeparam>
    /// <param name="jobId">Unique identifier for the job</param>
    /// <param name="methodCall">Expression defining the job method to call</param>
    /// <param name="cronExpression">Cron expression for scheduling</param>
    /// <param name="timeZone">Time zone for scheduling (default: UTC)</param>
    public static void AddRecurringJob<TJob>(
        string jobId,
        System.Linq.Expressions.Expression<Action<TJob>> methodCall,
        string cronExpression,
        TimeZoneInfo? timeZone = null)
    {
        RecurringJob.AddOrUpdate(
            jobId,
            methodCall,
            cronExpression,
            timeZone ?? TimeZoneInfo.Utc);
    }

    /// <summary>
    /// Creates the Hangfire database schema if it doesn't exist
    /// This is useful for initial setup in HA scenarios
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public static void EnsureHangfireDatabaseSchema(
        string connectionString,
        ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Ensuring Hangfire database schema exists");

            var options = new PostgreSqlStorageOptions
            {
                PrepareSchemaIfNecessary = true
            };

            using var storage = new PostgreSqlStorage(connectionString, options);

            logger?.LogInformation("Hangfire database schema verified");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to ensure Hangfire database schema");
            throw;
        }
    }
}
