# BahyWay SharedKernel - Complete Usage Guide

This guide shows how to integrate all SharedKernel infrastructure components into your BahyWay projects.

## Table of Contents
1. [Quick Start](#quick-start)
2. [Program.cs Configuration](#programcs-configuration)
3. [Project-Specific Examples](#project-specific-examples)
4. [Best Practices](#best-practices)

---

## Quick Start

### Step 1: Install Required Packages

See [NUGET_PACKAGES.md](./NUGET_PACKAGES.md) for the complete list.

```bash
# Essential Tier 1 packages
dotnet add package Serilog.AspNetCore
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.PostgreSql
dotnet add package StackExchange.Redis
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

### Step 2: Add SharedKernel Reference

```xml
<ItemGroup>
  <ProjectReference Include="..\BahyWay.SharedKernel\BahyWay.SharedKernel.csproj" />
</ItemGroup>
```

---

## Program.cs Configuration

### Complete Example: AlarmInsight Application

```csharp
using Serilog;
using BahyWay.SharedKernel.Infrastructure.Logging;
using BahyWay.SharedKernel.Infrastructure.BackgroundJobs;
using BahyWay.SharedKernel.Infrastructure.Caching;
using BahyWay.SharedKernel.Infrastructure.Audit;
using BahyWay.SharedKernel.Infrastructure.FileWatcher;
using BahyWay.SharedKernel.Infrastructure.HealthChecks;

// Create bootstrap logger for startup errors
Log.Logger = SerilogConfiguration.CreateBootstrapLogger("AlarmInsight");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ============================================================================
    // 1. LOGGING CONFIGURATION
    // ============================================================================
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        SerilogConfiguration.ConfigureBahyWayLogging(
            context.Configuration,
            "AlarmInsight",
            context.HostEnvironment);
    });

    // ============================================================================
    // 2. CORE SERVICES
    // ============================================================================
    
    // Correlation ID service for request tracking
    builder.Services.AddSingleton<ICorrelationIdService, CorrelationIdService>();
    
    // Application logger
    builder.Services.AddScoped(typeof(IApplicationLogger<>), typeof(ApplicationLogger<>));
    
    // Current user service for audit
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // ============================================================================
    // 3. DATABASE & ENTITY FRAMEWORK
    // ============================================================================
    
    builder.Services.AddDbContext<AlarmInsightDbContext>((serviceProvider, options) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("AlarmInsight.Infrastructure");
            npgsqlOptions.EnableRetryOnFailure(3);
        });

        // Add audit interceptor
        var logger = serviceProvider.GetRequiredService<IApplicationLogger<AuditInterceptor>>();
        var currentUser = serviceProvider.GetRequiredService<ICurrentUserService>();
        options.AddInterceptors(new AuditInterceptor(currentUser, logger));
    });

    // ============================================================================
    // 4. CACHING
    // ============================================================================
    
    // In-memory cache for development
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<ICacheService, MemoryCacheService>();
    }
    // Redis for production
    else
    {
        var redisConnection = builder.Configuration.GetConnectionString("Redis");
        builder.Services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnection));
        builder.Services.AddScoped<ICacheService, RedisCacheService>();
    }

    // ============================================================================
    // 5. BACKGROUND JOBS (HANGFIRE)
    // ============================================================================
    
    builder.Services.ConfigureBahyWayHangfire(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        "AlarmInsight");

    // ============================================================================
    // 6. HEALTH CHECKS
    // ============================================================================
    
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .AddRedis(builder.Configuration.GetConnectionString("Redis"))
        .AddCheck<FileSystemHealthCheck>("file-system", 
            new FileSystemHealthCheck("/var/alarms/data"));

    // ============================================================================
    // 7. RESILIENCY (POLLY)
    // ============================================================================
    
    builder.Services.AddHttpClient("resilient")
        .AddPolicyHandler(ResiliencePolicies.GetHttpRetryPolicy())
        .AddPolicyHandler(ResiliencePolicies.GetCircuitBreakerPolicy());

    // ============================================================================
    // 8. FILE STORAGE
    // ============================================================================
    
    builder.Services.AddSingleton(new FileStorageOptions
    {
        Provider = FileStorageProvider.Local,
        BasePath = builder.Configuration["Storage:BasePath"] ?? "/var/alarms/storage"
    });
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

    // ============================================================================
    // 9. FILE WATCHER (For alarm files)
    // ============================================================================
    
    builder.Services.AddSingleton(new FileWatcherOptions
    {
        WatchPath = builder.Configuration["Alarms:WatchPath"] ?? "/var/alarms/incoming",
        Filter = "*.alarm",
        StabilizationDelay = TimeSpan.FromSeconds(5),
        ProcessExistingFiles = true
    });
    builder.Services.AddSingleton<IFileWatcherService, FileWatcherService>();
    builder.Services.AddScoped<AlarmFileProcessingService>();
    builder.Services.AddHostedService<AlarmFileWatcherHostedService>();

    // ============================================================================
    // 10. APPLICATION SERVICES (MediatR, etc.)
    // ============================================================================
    
    builder.Services.AddMediatR(cfg => 
        cfg.RegisterServicesFromAssembly(typeof(ProcessAlarmCommand).Assembly));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // ============================================================================
    // BUILD APP
    // ============================================================================
    
    var app = builder.Build();

    // ============================================================================
    // MIDDLEWARE PIPELINE
    // ============================================================================
    
    // Correlation ID middleware
    app.Use(async (context, next) =>
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");
        
        var correlationService = context.RequestServices.GetRequiredService<ICorrelationIdService>();
        correlationService.SetCorrelationId(correlationId);
        
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        
        await next();
    });

    // Request logging
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    // Health check endpoints
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Hangfire Dashboard
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });

    // ============================================================================
    // CONFIGURE BACKGROUND JOBS
    // ============================================================================
    
    using (var scope = app.Services.CreateScope())
    {
        var backgroundJobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
        
        // Recurring job: Process pending alarms every 5 minutes
        backgroundJobs.AddOrUpdateRecurringJob(
            "process-pending-alarms",
            () => ProcessPendingAlarmsJob.Execute(),
            CronExpressions.Every5Minutes);

        // Recurring job: Cleanup old alarms daily at 2 AM
        backgroundJobs.AddOrUpdateRecurringJob(
            "cleanup-old-alarms",
            () => CleanupOldAlarmsJob.Execute(),
            CronExpressions.DailyAt2AM);
    }

    // ============================================================================
    // RUN APPLICATION
    // ============================================================================
    
    Log.Information("Starting AlarmInsight application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
```

---

## Project-Specific Examples

### ETLway Configuration

```csharp
// In Program.cs - ETLway specific configuration

// File Watcher for ZIP files
builder.Services.AddSingleton(new FileWatcherOptions
{
    WatchPath = "/var/etl/incoming",
    Filter = "*.zip",
    MinimumFileSizeBytes = 1024,  // 1KB minimum
    MaximumFileSizeBytes = 5L * 1024 * 1024 * 1024,  // 5GB maximum
    StabilizationDelay = TimeSpan.FromSeconds(10),
    ProcessExistingFiles = true
});

// Background jobs for ETL processing
backgroundJobs.AddOrUpdateRecurringJob(
    "etl-hourly-import",
    () => EtlHourlyImportJob.Execute(),
    CronExpressions.Hourly);
```

### SmartForesight Configuration

```csharp
// In Program.cs - SmartForesight specific configuration

// Caching for forecast results
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Background jobs for model training
backgroundJobs.AddOrUpdateRecurringJob(
    "retrain-forecast-models",
    () => RetrainForecastModelsJob.Execute(),
    CronExpressions.Daily);

// File storage for trained models
builder.Services.AddSingleton(new FileStorageOptions
{
    Provider = FileStorageProvider.Local,
    BasePath = "/var/forecasts/models"
});
```

### HireWay Configuration

```csharp
// In Program.cs - HireWay specific configuration

// File storage for resumes
builder.Services.AddSingleton(new FileStorageOptions
{
    Provider = FileStorageProvider.AzureBlob,  // Use cloud storage
    ConnectionString = builder.Configuration["Azure:Storage:ConnectionString"],
    ContainerName = "resumes"
});

// Background jobs for resume parsing
backgroundJobs.AddOrUpdateRecurringJob(
    "process-new-applications",
    () => ProcessNewApplicationsJob.Execute(),
    CronExpressions.Every15Minutes);

// Notification service for email
builder.Services.AddScoped<INotificationService, EmailNotificationService>();
```

### NajafCemetery Configuration

```csharp
// In Program.cs - NajafCemetery specific configuration

// PostGIS for geospatial data
builder.Services.AddDbContext<CemeteryDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseNetTopologySuite();  // Enable PostGIS
    }));

// Caching for map tiles and H3 indexes
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// File storage for burial photos/documents
builder.Services.AddSingleton(new FileStorageOptions
{
    Provider = FileStorageProvider.Local,
    BasePath = "/var/cemetery/documents"
});
```

---

## Best Practices

### 1. Logging

**DO:**
```csharp
_logger.LogInformation("Processing alarm {AlarmId} for location {Location}", alarmId, location);
```

**DON'T:**
```csharp
_logger.LogInformation($"Processing alarm {alarmId} for location {location}");
```

### 2. Caching

**DO:**
```csharp
var alarms = await _cacheService.GetOrCreateAsync(
    CacheKeys.Alarms.ByLocation(location),
    async () => await _alarmRepository.GetByLocationAsync(location),
    CacheExpiration.Medium);
```

**DON'T:**
```csharp
var alarms = await _alarmRepository.GetByLocationAsync(location);  // Always hits DB
```

### 3. Background Jobs

**DO:**
```csharp
// Fire-and-forget for non-critical tasks
_backgroundJobService.Enqueue<SendEmailJob>(job => job.SendAsync(emailId));

// Scheduled for specific timing
_backgroundJobService.Schedule<GenerateReportJob>(
    job => job.GenerateAsync(reportId),
    TimeSpan.FromHours(1));
```

**DON'T:**
```csharp
// Don't block HTTP requests with long-running operations
await SendEmailAsync(emailId);  // This blocks the request
```

### 4. File Watcher

**DO:**
```csharp
_fileWatcher.FileDetected += (sender, args) =>
{
    // Enqueue background job immediately, don't process inline
    _backgroundJobService.Enqueue<ProcessFileJob>(
        job => job.ProcessAsync(args.FilePath));
};
```

**DON'T:**
```csharp
_fileWatcher.FileDetected += async (sender, args) =>
{
    // Don't process files directly in event handler
    await ProcessFileAsync(args.FilePath);  // This blocks the watcher
};
```

### 5. Health Checks

**DO:**
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, tags: new[] { "ready", "db" })
    .AddRedis(redisConnection, tags: new[] { "ready", "cache" })
    .AddCheck<CustomHealthCheck>("custom", tags: new[] { "live" });
```

### 6. Audit Logging

**DO:**
```csharp
public class Alarm : AuditableEntity
{
    // Entity properties
}

// Audit fields are automatically populated by AuditInterceptor
```

### 7. Error Handling

**DO:**
```csharp
try
{
    await ProcessAlarmAsync(alarmId);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to process alarm {AlarmId}", alarmId);
    throw;
}
```

---

## Environment-Specific Configuration

### appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=alarmsight_dev;Username=dev;Password=dev",
    "Redis": "localhost:6379"
  },
  "Serilog": {
    "SeqServerUrl": "http://localhost:5341"
  },
  "Alarms": {
    "WatchPath": "C:\\dev\\alarms\\incoming"
  },
  "Storage": {
    "BasePath": "C:\\dev\\alarms\\storage"
  }
}
```

### appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.bahyway.com;Database=alarmsight_prod;Username=prod_user;Password=${DB_PASSWORD}",
    "Redis": "redis.bahyway.com:6379,password=${REDIS_PASSWORD}"
  },
  "Serilog": {
    "ElasticsearchUrl": "https://elk.bahyway.com"
  },
  "Alarms": {
    "WatchPath": "/var/alarms/incoming"
  },
  "Storage": {
    "BasePath": "/var/alarms/storage"
  }
}
```

---

## Docker Configuration

### docker-compose.yml for Development

```yaml
version: '3.8'

services:
  postgres:
    image: postgis/postgis:15-3.3
    environment:
      POSTGRES_DB: bahyway_dev
      POSTGRES_USER: dev
      POSTGRES_PASSWORD: dev
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes
    volumes:
      - redis_data:/data

  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest

  seq:
    image: datalust/seq:latest
    ports:
      - "5341:80"
    environment:
      ACCEPT_EULA: Y

volumes:
  postgres_data:
  redis_data:
```

---

## Next Steps

1. Review [IMPLEMENTATION_ROADMAP.md](../IMPLEMENTATION_ROADMAP.md) for the 12-week plan
2. Check [NUGET_PACKAGES.md](./NUGET_PACKAGES.md) for all required packages
3. Start with Phase 1: Observability (Week 1-3)
4. Test in development environment before production deployment
