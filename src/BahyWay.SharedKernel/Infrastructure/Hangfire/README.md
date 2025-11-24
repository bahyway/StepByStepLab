# Hangfire PostgreSQL HA Integration

This module provides seamless integration between Hangfire background job processing and PostgreSQL High Availability infrastructure.

## Features

- **High Availability Support**: Configures Hangfire to work optimally with PostgreSQL HA clusters
- **Health Monitoring Integration**: Built-in support for monitoring PostgreSQL cluster health via Hangfire jobs
- **Easy Configuration**: Simple extension methods for quick setup
- **Production-Ready**: Includes recommended settings for distributed systems

## Installation

The Hangfire integration is included in `BahyWay.SharedKernel`. No additional packages are required beyond:
- `Hangfire.Core`
- `Hangfire.AspNetCore`
- `Hangfire.PostgreSql`

These are already included as dependencies in SharedKernel.

## Basic Usage

### 1. Configure in Program.cs

```csharp
using BahyWay.SharedKernel.Infrastructure.Hangfire;
using BahyWay.SharedKernel.Infrastructure.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// Add PostgreSQL HA health monitoring
builder.Services.AddPostgreSQLHealthMonitoring();

// Add Hangfire with PostgreSQL storage
builder.Services.AddHangfireWithPostgreSQL(
    builder.Configuration,
    connectionStringName: "HangfireConnection",
    serverName: "MyApplication",
    workerCount: Environment.ProcessorCount * 5);

var app = builder.Build();

// Add Hangfire dashboard
app.UseHangfireDashboard("/hangfire");
```

### 2. Configure Connection String

In `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "HangfireConnection": "Host=localhost;Port=5432;Database=myapp_hangfire;Username=hangfire_user;Password=hangfire_pass"
  }
}
```

### 3. Create Background Jobs

Create a job class:

```csharp
public class MyBackgroundJob
{
    private readonly ILogger<MyBackgroundJob> _logger;

    public MyBackgroundJob(ILogger<MyBackgroundJob> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync()
    {
        _logger.LogInformation("Processing background job");
        // Your job logic here
        await Task.CompletedTask;
    }
}
```

### 4. Schedule Jobs

```csharp
using Hangfire;

// Fire-and-forget job
BackgroundJob.Enqueue<MyBackgroundJob>(job => job.ProcessAsync());

// Delayed job
BackgroundJob.Schedule<MyBackgroundJob>(
    job => job.ProcessAsync(),
    TimeSpan.FromHours(1));

// Recurring job
RecurringJob.AddOrUpdate<MyBackgroundJob>(
    "my-job-id",
    job => job.ProcessAsync(),
    Cron.Daily);
```

## Advanced Usage

### PostgreSQL Health Monitoring Job

Create a health monitoring job:

```csharp
using BahyWay.SharedKernel.Application.Abstractions;

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

    public async Task MonitorHealthAsync()
    {
        var health = await _healthService.GetClusterHealthAsync();

        var isHealthy = health.TryGetValue("IsHealthy", out var status)
            && status is bool healthy && healthy;

        if (!isHealthy)
        {
            _logger.LogWarning("PostgreSQL cluster is unhealthy: {Health}", health);
        }
    }
}
```

Schedule it after app startup:

```csharp
var app = builder.Build();
app.UseHangfireDashboard("/hangfire");

// Schedule recurring health checks
RecurringJob.AddOrUpdate<PostgreSQLHealthMonitorJob>(
    "postgresql-health-monitor",
    job => job.MonitorHealthAsync(),
    Cron.MinuteInterval(5));

app.Run();
```

### Custom Configuration

For more control, use the advanced configuration:

```csharp
builder.Services.AddHangfireWithPostgreSQLAndHealthMonitoring(
    builder.Configuration,
    scheduleHealthMonitoring: true,
    healthCheckCron: "*/5 * * * *",  // Every 5 minutes
    connectionStringName: "HangfireConnection",
    serverName: "MyApplication-Worker-1",
    workerCount: 10);
```

### Ensure Database Schema

For initial setup or deployment automation:

```csharp
using BahyWay.SharedKernel.Infrastructure.Hangfire;

var connectionString = configuration.GetConnectionString("HangfireConnection");
HangfirePostgreSQLExtensions.EnsureHangfireDatabaseSchema(
    connectionString,
    logger);
```

## Configuration Options

### HangfireWithPostgreSQL Options

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `connectionStringName` | string | "HangfireConnection" | Name of connection string in configuration |
| `serverName` | string | Machine name | Unique name for this Hangfire server instance |
| `workerCount` | int | ProcessorCount * 5 | Number of concurrent background workers |

### HA-Specific Settings

The integration automatically configures these settings for HA scenarios:

- **HeartbeatInterval**: 30 seconds (default: 30s)
- **ServerCheckInterval**: 1 minute (default: 1m)
- **ServerTimeout**: 5 minutes (default: 5m)
- **SchedulePollingInterval**: 15 seconds (default: 15s)

These settings ensure proper failover and load balancing in distributed deployments.

## High Availability Considerations

### Database Setup

1. **Dedicated Database**: Use a separate database for Hangfire to isolate job processing from application data:
   ```sql
   CREATE DATABASE myapp_hangfire;
   CREATE USER hangfire_user WITH PASSWORD 'secure_password';
   GRANT ALL PRIVILEGES ON DATABASE myapp_hangfire TO hangfire_user;
   ```

2. **Replication**: Ensure Hangfire database is replicated to all nodes in your HA cluster

3. **Connection Pooling**: Configure appropriate pool sizes in connection string:
   ```
   Host=localhost;Port=5432;Database=myapp_hangfire;
   Username=hangfire_user;Password=secure_password;
   Minimum Pool Size=5;Maximum Pool Size=100;
   ```

### Multiple Server Instances

When running multiple Hangfire servers:

1. **Unique Server Names**: Each instance should have a unique `serverName`
2. **Shared Storage**: All instances must connect to the same PostgreSQL database
3. **Load Distribution**: Hangfire automatically distributes jobs across available servers
4. **Automatic Failover**: If one server fails, others pick up its jobs automatically

### Monitoring

Monitor your Hangfire setup:

1. **Dashboard**: Access at `/hangfire` endpoint
2. **Health Checks**: Use PostgreSQL HA health monitoring
3. **Metrics**: Monitor job success/failure rates
4. **Alerts**: Set up alerts for failed jobs or unhealthy clusters

## Example: Complete Integration

See the `AlarmInsight.API` example project for a complete working implementation including:
- Full Hangfire setup with PostgreSQL
- PostgreSQL HA health monitoring
- Recurring background jobs
- API endpoints for health checks
- Swagger documentation

## Troubleshooting

### Connection Failures

If Hangfire can't connect to PostgreSQL:

1. Verify connection string in configuration
2. Check PostgreSQL is running and accessible
3. Verify user permissions on Hangfire database
4. Check firewall rules

### Schema Migration Issues

If you see schema-related errors:

```csharp
// Manually ensure schema exists
HangfirePostgreSQLExtensions.EnsureHangfireDatabaseSchema(
    connectionString,
    logger);
```

### Performance Issues

If jobs are processing slowly:

1. Increase `workerCount`
2. Add more server instances
3. Optimize database queries in jobs
4. Check PostgreSQL cluster health
5. Monitor replication lag

## References

- [Hangfire Documentation](https://docs.hangfire.io)
- [Hangfire.PostgreSql](https://github.com/frankhommers/Hangfire.PostgreSql)
- BahyWay PostgreSQL HA Module Documentation
