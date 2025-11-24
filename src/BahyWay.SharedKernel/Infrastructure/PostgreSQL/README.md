# PostgreSQL HA Health Monitoring Service

## Overview

The PostgreSQL HA Health Monitoring Service provides comprehensive health monitoring capabilities for PostgreSQL High Availability (HA) clusters. It integrates with the BahyWay PostgreSQL HA PowerShell module to provide C# applications with real-time health monitoring, replication status tracking, and alarm management.

## Features

- **Cluster Health Monitoring**: Get comprehensive health status of your PostgreSQL HA cluster
- **Replication Status**: Monitor replication lag and status between primary and replica nodes
- **Docker Environment Testing**: Verify Docker environment configuration
- **Node Health Checks**: Individual health checks for primary and replica nodes
- **Database Metrics**: Monitor database size and connection counts
- **Alarm Management**: Retrieve and manage health alarms
- **PowerShell Integration**: Execute any command from the BahyWay PostgreSQL HA module

## Installation

### 1. Prerequisites

The service requires the BahyWay PostgreSQL HA PowerShell module to be present at one of these locations:

- `{AppBaseDirectory}/PowerShellModules/BahyWay.PostgreSQLHA/` (for deployment)
- `infrastructure/postgresql-ha/powershell-module/BahyWay.PostgreSQLHA/` (for development)

### 2. Package Dependencies

The following NuGet packages are required (already included in BahyWay.SharedKernel):

```xml
<PackageReference Include="Microsoft.PowerShell.SDK" Version="7.4.0" />
```

### 3. Service Registration

Register the service in your application's dependency injection container:

```csharp
using BahyWay.SharedKernel.Infrastructure.PostgreSQL;

// In Program.cs or Startup.cs
builder.Services.AddPostgreSQLHealthMonitoring();
```

## Usage

### Basic Usage

Inject `IPostgreSQLHealthService` into your controllers or services:

```csharp
using BahyWay.SharedKernel.Application.Abstractions;

public class DatabaseHealthController : ControllerBase
{
    private readonly IPostgreSQLHealthService _healthService;

    public DatabaseHealthController(IPostgreSQLHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetClusterHealth()
    {
        var health = await _healthService.GetClusterHealthAsync();
        return Ok(health);
    }
}
```

### Available Methods

#### Get Cluster Health

```csharp
var health = await _healthService.GetClusterHealthAsync(
    includeHAProxy: true,
    includeBarman: true
);

Console.WriteLine($"Cluster Healthy: {health["IsHealthy"]}");
Console.WriteLine($"Primary Status: {health["Primary"]}");
Console.WriteLine($"Replica Status: {health["Replica"]}");
```

#### Test Docker Environment

```csharp
var dockerStatus = await _healthService.TestDockerEnvironmentAsync();

if ((bool)dockerStatus["IsHealthy"])
{
    Console.WriteLine("Docker environment is healthy");
}
```

#### Test Primary Node

```csharp
var primaryHealth = await _healthService.TestPrimaryNodeAsync();

Console.WriteLine($"Primary Node Healthy: {primaryHealth["IsHealthy"]}");
Console.WriteLine($"Uptime: {primaryHealth["Uptime"]}");
```

#### Test Replica Node

```csharp
var replicaHealth = await _healthService.TestReplicaNodeAsync();

Console.WriteLine($"Replica Node Healthy: {replicaHealth["IsHealthy"]}");
Console.WriteLine($"Replication Status: {replicaHealth["ReplicationStatus"]}");
```

#### Check Replication Status

```csharp
var replicationStatus = await _healthService.TestReplicationStatusAsync();

Console.WriteLine($"Replication Healthy: {replicationStatus["IsHealthy"]}");
Console.WriteLine($"Replication Lag: {replicationStatus["LagSeconds"]} seconds");
```

#### Get Replication Lag

```csharp
var lag = await _healthService.GetReplicationLagAsync();

Console.WriteLine($"Lag in Bytes: {lag["LagBytes"]}");
Console.WriteLine($"Lag in Seconds: {lag["LagSeconds"]}");
```

#### Get Database Size

```csharp
// Get all databases
var allDbSizes = await _healthService.GetDatabaseSizeAsync();

// Get specific database
var specificDbSize = await _healthService.GetDatabaseSizeAsync("my_database");
```

#### Get Connection Count

```csharp
var connections = await _healthService.GetConnectionCountAsync();

Console.WriteLine($"Active Connections: {connections["ActiveConnections"]}");
Console.WriteLine($"Max Connections: {connections["MaxConnections"]}");
```

#### Manage Health Alarms

```csharp
// Get all alarms
var alarms = await _healthService.GetHealthAlarmsAsync();

foreach (var alarm in alarms)
{
    Console.WriteLine($"Alarm: {alarm["Message"]} - Severity: {alarm["Severity"]}");
}

// Clear alarms
await _healthService.ClearHealthAlarmsAsync();
```

#### Execute Custom PowerShell Commands

```csharp
var parameters = new Dictionary<string, object>
{
    ["ContainerName"] = "my-postgres-primary"
};

var result = await _healthService.InvokePowerShellAsync(
    "Test-PostgreSQLPrimary",
    parameters
);
```

## Example: Health Check Endpoint

Here's a complete example of a health check API endpoint:

```csharp
using Microsoft.AspNetCore.Mvc;
using BahyWay.SharedKernel.Application.Abstractions;

namespace MyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseHealthController : ControllerBase
{
    private readonly IPostgreSQLHealthService _healthService;
    private readonly ILogger<DatabaseHealthController> _logger;

    public DatabaseHealthController(
        IPostgreSQLHealthService healthService,
        ILogger<DatabaseHealthController> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    [HttpGet("cluster")]
    public async Task<IActionResult> GetClusterHealth()
    {
        try
        {
            var health = await _healthService.GetClusterHealthAsync();

            if (!(bool)health["IsHealthy"])
            {
                _logger.LogWarning("PostgreSQL cluster is unhealthy");
                return StatusCode(503, health);
            }

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cluster health");
            return StatusCode(500, new { error = "Failed to retrieve health status" });
        }
    }

    [HttpGet("replication")]
    public async Task<IActionResult> GetReplicationStatus()
    {
        try
        {
            var status = await _healthService.TestReplicationStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get replication status");
            return StatusCode(500, new { error = "Failed to retrieve replication status" });
        }
    }

    [HttpGet("alarms")]
    public async Task<IActionResult> GetAlarms()
    {
        try
        {
            var alarms = await _healthService.GetHealthAlarmsAsync();
            return Ok(alarms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health alarms");
            return StatusCode(500, new { error = "Failed to retrieve alarms" });
        }
    }

    [HttpPost("alarms/clear")]
    public async Task<IActionResult> ClearAlarms()
    {
        try
        {
            var result = await _healthService.ClearHealthAlarmsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear health alarms");
            return StatusCode(500, new { error = "Failed to clear alarms" });
        }
    }
}
```

## Background Job Integration

You can also integrate the health service with Hangfire for scheduled monitoring:

```csharp
using Hangfire;
using BahyWay.SharedKernel.Application.Abstractions;

public class PostgreSQLHealthMonitorJob
{
    private readonly IPostgreSQLHealthService _healthService;
    private readonly IApplicationLogger<PostgreSQLHealthMonitorJob> _logger;

    public PostgreSQLHealthMonitorJob(
        IPostgreSQLHealthService healthService,
        IApplicationLogger<PostgreSQLHealthMonitorJob> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task MonitorClusterHealth()
    {
        var health = await _healthService.GetClusterHealthAsync();

        if (!(bool)health["IsHealthy"])
        {
            _logger.LogWarning("PostgreSQL cluster health check failed", health);

            // Get alarms
            var alarms = await _healthService.GetHealthAlarmsAsync();

            foreach (var alarm in alarms)
            {
                _logger.LogError("PostgreSQL Alarm: {Message}", alarm["Message"]);
            }
        }
    }
}

// Schedule the job in Program.cs
RecurringJob.AddOrUpdate<PostgreSQLHealthMonitorJob>(
    "monitor-postgresql-health",
    job => job.MonitorClusterHealth(),
    Cron.Every5Minutes()
);
```

## Troubleshooting

### Module Not Found Error

If you get a "PowerShell module not found" error:

1. Ensure the PowerShell module files are in one of the expected locations
2. Check file permissions on the module files
3. Verify the module files have been copied to the output directory (set "Copy to Output Directory" to "Copy if newer")

### PowerShell Execution Policy Error

If you get an execution policy error:

- The service sets ExecutionPolicy to RemoteSigned automatically
- Ensure your environment allows RemoteSigned scripts
- On Linux/Mac, this typically isn't an issue

### Docker Not Accessible

If Docker environment tests fail:

1. Ensure Docker daemon is running
2. Verify Docker is accessible from your application's context
3. Check Docker permissions for the application user

## Performance Considerations

- The service uses a single PowerShell runspace per instance
- The runspace is reused for multiple commands, improving performance
- Consider using scoped lifetime (default) for web applications
- For background jobs, consider using a singleton pattern with proper locking

## Security

- The service requires access to Docker and PostgreSQL containers
- Logs may contain sensitive database information
- Consider implementing authentication for health check endpoints in production
- Use appropriate access controls for alarm logs

## License

Copyright (c) 2025 BahyWay Solutions. All rights reserved.
