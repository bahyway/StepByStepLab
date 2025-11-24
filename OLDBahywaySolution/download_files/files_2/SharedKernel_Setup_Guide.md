# BahyWay SharedKernel - Observability & FileWatcher Setup Guide

## Quick Start

This guide shows how to integrate all the observability components and FileWatcher service into your BahyWay projects.

---

## 1. Add NuGet Packages to Your Project

```bash
# Observability - Logging
dotnet add package Serilog.AspNetCore --version 8.0.0
dotnet add package Serilog.Sinks.Console --version 5.0.1
dotnet add package Serilog.Sinks.File --version 5.0.0
dotnet add package Serilog.Sinks.Seq --version 6.0.0
dotnet add package Serilog.Enrichers.Environment --version 2.3.0
dotnet add package Serilog.Enrichers.Thread --version 3.1.0
dotnet add package Serilog.Exceptions --version 8.4.0

# Observability - Metrics
dotnet add package OpenTelemetry.Extensions.Hosting --version 1.7.0
dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version 1.7.0
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore --version 1.7.0-rc.1

# Health Checks
dotnet add package AspNetCore.HealthChecks.Npgsql --version 8.0.0
dotnet add package AspNetCore.HealthChecks.Redis --version 8.0.0
dotnet add package AspNetCore.HealthChecks.RabbitMQ --version 8.0.0

# Redis (for caching and health checks)
dotnet add package StackExchange.Redis --version 2.7.10
```

---

## 2. Update appsettings.json

```json
{
  "Application": {
    "Name": "AlarmInsight"
  },
  "Logging": {
    "MinimumLevel": "Information",
    "File": {
      "Path": "logs/log-.txt",
      "RetainedFileCount": 31
    },
    "Seq": {
      "ServerUrl": "http://localhost:5341",
      "ApiKey": ""
    },
    "Elasticsearch": {
      "NodeUris": "http://localhost:9200",
      "IndexFormat": "bahyway-alarminsight-{0:yyyy.MM.dd}"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=AlarmInsight;Username=postgres;Password=yourpassword",
    "Redis": "localhost:6379",
    "RabbitMQ": "amqp://guest:guest@localhost:5672"
  },
  "FileWatcher": {
    "MaxConcurrentProcessing": 5,
    "WatchDirectories": [
      {
        "Name": "Alarm_Historical",
        "Path": "/data/alarms/inbox",
        "FilePattern": "*.csv",
        "ProcessExistingFiles": true,
        "AutoExtractZip": true,
        "ProcessedPath": "/data/alarms/processed",
        "ErrorPath": "/data/alarms/error",
        "FileReadyTimeout": 60,
        "CalculateHash": true,
        "CustomMetadata": {
          "DataType": "Historical",
          "Source": "SCADA"
        }
      }
    ]
  }
}
```

---

## 3. Update Program.cs

```csharp
using SharedKernel.Infrastructure.Observability.Logging;
using SharedKernel.Infrastructure.Observability.Metrics;
using SharedKernel.Infrastructure.Observability.HealthChecks;
using SharedKernel.Infrastructure.FileWatcher;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using OpenTelemetry.Metrics;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. CONFIGURE LOGGING (Serilog)
// ============================================
builder.ConfigureSerilog(builder.Configuration);

// ============================================
// 2. ADD SERVICES
// ============================================

// MVC/API Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpContextAccessor for CorrelationId
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();

// ============================================
// 3. METRICS COLLECTION
// ============================================
var applicationName = builder.Configuration.GetValue<string>("Application:Name") ?? "BahyWay";
builder.Services.AddBahyWayMetrics(applicationName);

// OpenTelemetry Metrics
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter($"BahyWay.{applicationName}")
            .AddMeter($"BahyWay.{applicationName}.Business")
            .AddPrometheusExporter();
    });

// ============================================
// 4. HEALTH CHECKS
// ============================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var redisConnection = builder.Configuration.GetConnectionString("Redis");
var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMQ");

builder.Services.AddHealthChecks()
    .AddDatabaseHealthCheck(connectionString!, "database", tags: new[] { "db", "postgres" })
    .AddCheck<RedisHealthCheck>("redis", tags: new[] { "cache", "redis" })
    .AddRabbitMqHealthCheck(rabbitMqConnection!, "rabbitmq", tags: new[] { "messaging" })
    .AddFileSystemHealthCheck("/data", "filesystem", tags: new[] { "storage" });

// Add Redis connection for health checks and caching
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnection!);
    return ConnectionMultiplexer.Connect(configuration);
});

// ============================================
// 5. FILE WATCHER SERVICE
// ============================================
builder.Services.Configure<FileWatcherConfiguration>(
    builder.Configuration.GetSection("FileWatcher"));

// Register your file processor implementation
builder.Services.AddScoped<IFileProcessor, YourFileProcessor>();

// Add FileWatcher as hosted service
builder.Services.AddHostedService<FileWatcherService>();

// ============================================
// 6. BUILD APP
// ============================================
var app = builder.Build();

// ============================================
// 7. CONFIGURE MIDDLEWARE PIPELINE
// ============================================

// Correlation ID (FIRST - so it's available to all subsequent middleware)
app.UseCorrelationId();

// Request Logging (SECOND - logs with correlation ID)
app.UseRequestLogging(options =>
{
    options.LogDetailedInfo = app.Environment.IsDevelopment();
    options.LogRequestBody = false; // Enable only in dev if needed
    options.LogResponseBody = false;
});

// Metrics collection
app.UseMetrics();

// Swagger (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// ============================================
// 8. HEALTH CHECK ENDPOINTS
// ============================================
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Just checks if app is running
});

// ============================================
// 9. METRICS ENDPOINT (Prometheus)
// ============================================
app.MapPrometheusScrapingEndpoint();

// ============================================
// 10. RUN APP
// ============================================
try
{
    app.Logger.LogInformation("Starting {ApplicationName}...", applicationName);
    app.Run();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Application failed to start");
    throw;
}
finally
{
    LoggingConfiguration.CloseAndFlush();
}
```

---

## 4. Implement IFileProcessor

Create your file processor implementation in your Application layer:

```csharp
using SharedKernel.Infrastructure.FileWatcher;
using Microsoft.Extensions.Logging;

namespace AlarmInsight.Application.Services;

public class AlarmFileProcessor : IFileProcessor
{
    private readonly ILogger<AlarmFileProcessor> _logger;
    private readonly IMediator _mediator;
    private readonly MetricsCollector _metrics;

    public AlarmFileProcessor(
        ILogger<AlarmFileProcessor> logger,
        IMediator mediator,
        MetricsCollector metrics)
    {
        _logger = logger;
        _mediator = mediator;
        _metrics = metrics;
    }

    public async Task ProcessFileAsync(FileMetadata metadata, WatchDirectoryConfiguration config)
    {
        _logger.LogInformation(
            "Processing file: {FileName}, Size: {FileSize} bytes, Watcher: {WatcherName}",
            metadata.FileName,
            metadata.FileSize,
            metadata.WatcherName);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Your processing logic here
            switch (Path.GetExtension(metadata.FileName).ToLower())
            {
                case ".csv":
                    await ProcessCsvFileAsync(metadata, config);
                    break;
                
                case ".xlsx":
                    await ProcessExcelFileAsync(metadata, config);
                    break;

                default:
                    _logger.LogWarning("Unsupported file type: {FileName}", metadata.FileName);
                    break;
            }

            stopwatch.Stop();

            // Record metrics
            _metrics.RecordBackgroundJob(
                $"FileProcess_{config.Name}",
                stopwatch.Elapsed.TotalMilliseconds,
                success: true);

            _logger.LogInformation(
                "Successfully processed {FileName} in {ElapsedMs}ms",
                metadata.FileName,
                stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _metrics.RecordBackgroundJob(
                $"FileProcess_{config.Name}",
                stopwatch.Elapsed.TotalMilliseconds,
                success: false);

            _logger.LogError(ex, "Failed to process file: {FileName}", metadata.FileName);
            throw;
        }
    }

    private async Task ProcessCsvFileAsync(FileMetadata metadata, WatchDirectoryConfiguration config)
    {
        // Example: Parse CSV and send command to process alarms
        var command = new ImportAlarmsFromCsvCommand
        {
            FilePath = metadata.FilePath,
            FileName = metadata.FileName,
            FileHash = metadata.FileHash
        };

        await _mediator.Send(command);
    }

    private async Task ProcessExcelFileAsync(FileMetadata metadata, WatchDirectoryConfiguration config)
    {
        // Your Excel processing logic
        await Task.CompletedTask;
    }
}
```

---

## 5. Using Observability in Your Code

### Structured Logging with Correlation

```csharp
public class AlarmService
{
    private readonly ILogger<AlarmService> _logger;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public AlarmService(
        ILogger<AlarmService> logger,
        ICorrelationIdAccessor correlationIdAccessor)
    {
        _logger = logger;
        _correlationIdAccessor = correlationIdAccessor;
    }

    public async Task ProcessAlarmAsync(Alarm alarm)
    {
        var correlationId = _correlationIdAccessor.GetCorrelationId();
        
        _logger.LogInformation(
            "Processing alarm {AlarmId} with correlation {CorrelationId}",
            alarm.Id,
            correlationId);

        // Your business logic
        
        _logger.LogInformation(
            "Completed processing alarm {AlarmId}",
            alarm.Id);
    }
}
```

### Recording Business Metrics

```csharp
public class AlarmCommandHandler : IRequestHandler<ProcessAlarmCommand, Result>
{
    private readonly MetricsCollector _metrics;
    private readonly BahyWayBusinessMetrics.AlarmInsightMetrics _businessMetrics;

    public async Task<Result> Handle(ProcessAlarmCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Process alarm
            var alarm = await _repository.GetByIdAsync(request.AlarmId);
            
            // Apply rules, etc.
            
            // Record business metrics
            _businessMetrics.RecordAlarmProcessed(
                alarm.Severity,
                alarm.Location);

            if (alarm.RequiresEscalation)
            {
                _businessMetrics.RecordAlarmEscalated("High Priority");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process alarm {AlarmId}", request.AlarmId);
            return Result.Failure("Processing failed");
        }
    }
}
```

---

## 6. Accessing Observability Endpoints

Once your application is running:

- **Health Checks:** http://localhost:5000/health
- **Ready Check:** http://localhost:5000/health/ready
- **Live Check:** http://localhost:5000/health/live
- **Metrics (Prometheus):** http://localhost:5000/metrics
- **Swagger API Docs:** http://localhost:5000/swagger (Development only)

---

## 7. Setting Up Supporting Infrastructure

### Seq (Log Aggregation)

```bash
docker run -d \
  --name seq \
  -p 5341:80 \
  -e ACCEPT_EULA=Y \
  -v /path/to/seq/data:/data \
  datalust/seq:latest
```

Access Seq UI at: http://localhost:5341

### Redis (Caching)

```bash
docker run -d \
  --name redis \
  -p 6379:6379 \
  redis:latest
```

### Grafana + Prometheus (Metrics Visualization)

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - grafana-data:/var/lib/grafana

volumes:
  prometheus-data:
  grafana-data:
```

Create `prometheus.yml`:

```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'bahyway-alarminsight'
    static_configs:
      - targets: ['host.docker.internal:5000']
```

Run: `docker-compose up -d`

Access:
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)

---

## 8. Testing Your Setup

### Test Health Checks

```bash
curl http://localhost:5000/health
```

Expected response:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "description": "PostgreSQL is healthy (Version: 15.0)"
    },
    "redis": {
      "status": "Healthy"
    }
  }
}
```

### Test FileWatcher

1. Create watch directory: `mkdir -p /data/alarms/inbox`
2. Copy a test CSV file: `cp test-alarms.csv /data/alarms/inbox/`
3. Check logs: File should be detected and processed
4. Check processed folder: File should be moved to `/data/alarms/processed/`

### Test Metrics

```bash
curl http://localhost:5000/metrics
```

You should see Prometheus-formatted metrics.

---

## 9. Troubleshooting

### Logs not appearing?

Check:
- Serilog configuration in appsettings.json
- Log file path has write permissions
- Seq is running (if configured)

### FileWatcher not detecting files?

Check:
- Watch directory exists
- Correct file pattern (*.csv, *.zip, etc.)
- File permissions
- Check logs for errors

### Health checks failing?

Check:
- Database connection string
- Redis is running
- RabbitMQ is running
- Network connectivity

---

## 10. Next Steps

Now that you have Observability and FileWatcher set up:

1. **Week 3:** Add Background Jobs (Hangfire)
2. **Week 4:** Add Caching Infrastructure
3. **Week 5:** Add Event Bus (MassTransit + RabbitMQ)
4. **Week 6:** Add Audit Logging

See the main roadmap document for the complete 16-week implementation plan.

---

**Questions?** Check the individual component files for detailed documentation and examples.
