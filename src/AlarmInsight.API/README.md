# AlarmInsight.API

Example API project demonstrating PostgreSQL HA integration with BahyWay.SharedKernel and Hangfire.

## Features

- **PostgreSQL HA Health Monitoring**: Real-time monitoring of PostgreSQL cluster health, replication status, and performance metrics
- **Hangfire Integration**: Background job processing with PostgreSQL storage for high availability
- **RESTful API**: Comprehensive endpoints for health monitoring and cluster management
- **Swagger Documentation**: Interactive API documentation

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL HA cluster (primary + replica)
- Docker (for PowerShell module execution)

## Configuration

Update `appsettings.json` with your PostgreSQL connection strings:

```json
{
  "ConnectionStrings": {
    "HangfireConnection": "Host=localhost;Port=5432;Database=alarminsight_hangfire;Username=hangfire_user;Password=hangfire_pass",
    "AlarmInsightConnection": "Host=localhost;Port=5432;Database=alarminsight;Username=alarminsight_user;Password=alarminsight_pass"
  }
}
```

## Running the Application

```bash
cd src/AlarmInsight.API
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`
- Hangfire Dashboard: `https://localhost:5001/hangfire`

## API Endpoints

### Health Monitoring

- `GET /api/postgresql/health` - Get comprehensive cluster health status
- `GET /api/postgresql/healthz` - Kubernetes-style health check endpoint
- `GET /api/postgresql/docker` - Test Docker environment
- `GET /api/postgresql/primary` - Test primary node health
- `GET /api/postgresql/replica` - Test replica node health

### Replication Monitoring

- `GET /api/postgresql/replication` - Get replication status
- `GET /api/postgresql/replication/lag` - Get replication lag metrics

### Database Metrics

- `GET /api/postgresql/database/size` - Get database size information
- `GET /api/postgresql/connections` - Get active connection count

### Alarm Management

- `GET /api/postgresql/alarms` - Get all health alarms
- `POST /api/postgresql/alarms/clear` - Clear all health alarms

## Hangfire Background Jobs

The application automatically schedules the following recurring jobs:

- **PostgreSQL Health Monitor**: Runs every 5 minutes to check cluster health

### Manual Job Scheduling

You can schedule additional jobs via the Hangfire dashboard or programmatically:

```csharp
RecurringJob.AddOrUpdate<PostgreSQLHealthMonitorJob>(
    "replication-monitor",
    job => job.MonitorReplicationAsync(),
    Cron.Minutely());
```

## Example Usage

### Check Cluster Health

```bash
curl https://localhost:5001/api/postgresql/health?includeHAProxy=false&includeBarman=false
```

### Get Replication Lag

```bash
curl https://localhost:5001/api/postgresql/replication/lag
```

### View Health Alarms

```bash
curl https://localhost:5001/api/postgresql/alarms
```

## Integration with Other Services

This example shows how to integrate PostgreSQL HA monitoring into any ASP.NET Core application:

1. **Add Package Reference**:
   ```xml
   <ProjectReference Include="..\BahyWay.SharedKernel\BahyWay.SharedKernel.csproj" />
   ```

2. **Register Service** in `Program.cs`:
   ```csharp
   builder.Services.AddPostgreSQLHealthMonitoring();
   ```

3. **Inject and Use**:
   ```csharp
   public class MyService
   {
       private readonly IPostgreSQLHealthService _healthService;

       public MyService(IPostgreSQLHealthService healthService)
       {
           _healthService = healthService;
       }

       public async Task CheckHealthAsync()
       {
           var health = await _healthService.GetClusterHealthAsync();
           // Process health data...
       }
   }
   ```

## Production Considerations

1. **Authentication**: Implement proper authentication for the Hangfire dashboard
2. **Monitoring**: Set up alerts based on health check results
3. **Logging**: Configure structured logging for production
4. **Connection Pooling**: Configure appropriate connection pool settings
5. **High Availability**: Deploy multiple instances behind a load balancer

## Troubleshooting

### PowerShell Module Not Found

Ensure the BahyWay.PostgreSQLHA PowerShell module is available at:
- `{AppDirectory}/PowerShellModules/BahyWay.PostgreSQLHA/`
- OR in the repository at: `infrastructure/postgresql-ha/powershell-module/BahyWay.PostgreSQLHA/`

### Docker Connection Issues

Verify Docker is running and accessible:
```bash
docker ps
```

### Database Connection Issues

Test the connection string:
```bash
psql "Host=localhost;Port=5432;Database=alarminsight_hangfire;Username=hangfire_user"
```

## License

Part of the BahyWay StepByStepLab project.
