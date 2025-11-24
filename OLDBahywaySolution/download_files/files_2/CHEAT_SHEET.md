# BahyWay SharedKernel - Quick Reference Cheat Sheet

## 🚀 Essential Code Snippets

### Logging

```csharp
// Inject logger
private readonly IApplicationLogger<MyService> _logger;

// Log with structured data
_logger.LogInformation("Processing {EntityType} with ID {EntityId}", entityType, entityId);

// Log with properties
_logger.LogInformationWithProperties("User action completed", new Dictionary<string, object>
{
    ["UserId"] = userId,
    ["Action"] = "Delete",
    ["Success"] = true
});

// Log errors
_logger.LogError(exception, "Failed to process {EntityId}", entityId);
```

### Caching

```csharp
// Get or create
var data = await _cache.GetOrCreateAsync(
    CacheKeys.BuildKey("entity", entityId),
    () => LoadFromDatabaseAsync(entityId),
    CacheExpiration.Medium);

// Set manually
await _cache.SetAsync("my-key", data, CacheExpiration.Long);

// Remove
await _cache.RemoveAsync("my-key");

// Remove by pattern
await _cache.RemoveByPatternAsync("user:*");
```

### Background Jobs

```csharp
// Fire and forget
_jobs.Enqueue<MyJob>(job => job.ExecuteAsync());

// Delayed
_jobs.Schedule<MyJob>(job => job.ExecuteAsync(), TimeSpan.FromHours(1));

// Recurring
_jobs.AddOrUpdateRecurringJob(
    "daily-cleanup",
    () => CleanupJob.Execute(),
    CronExpressions.Daily);

// Continuation
var parentId = _jobs.Enqueue<ParentJob>(job => job.ExecuteAsync());
_jobs.ContinueWith(parentId, () => ChildJob.Execute());
```

### File Watcher

```csharp
// Subscribe to events
_fileWatcher.FileDetected += (sender, args) =>
{
    _logger.LogInformation("File detected: {FileName}", args.FileName);
    _jobs.Enqueue<ProcessFileJob>(job => job.ProcessAsync(args.FilePath));
};

// Start watching
await _fileWatcher.StartAsync();
```

### File Storage

```csharp
// Upload
using var stream = File.OpenRead("local-file.pdf");
var path = await _storage.UploadAsync(stream, "document.pdf", "uploads");

// Download
var stream = await _storage.DownloadAsync(path);

// Check existence
if (await _storage.ExistsAsync(path))
{
    await _storage.DeleteAsync(path);
}

// List files
var files = await _storage.ListFilesAsync("uploads");
```

### Audit Entity

```csharp
public class MyEntity : AuditableEntity
{
    public string Name { get; set; }
    
    // CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy
    // are automatically populated
}

// Soft delete support
public class MyEntity : SoftDeletableAuditableEntity
{
    public string Name { get; set; }
    
    // IsDeleted, DeletedAt, DeletedBy
    // + audit fields automatically populated
}
```

### Health Checks

```csharp
// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddRedis(redisConnection)
    .AddCheck<CustomHealthCheck>("my-check");

// Map endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
```

---

## 📦 Common Cache Keys

```csharp
// Alarms
CacheKeys.Alarms.ById(123)                    // "alarm:123"
CacheKeys.Alarms.ByLocation("Station-A")      // "alarm:location:Station-A"
CacheKeys.Alarms.Pattern()                    // "alarm:*"

// Forecasts
CacheKeys.Forecasts.ById(456)                 // "forecast:456"
CacheKeys.Forecasts.ByModel("SARIMA")         // "forecast:model:SARIMA"

// GeoSpatial
CacheKeys.GeoSpatial.ByH3Index("8928308")     // "geo:h3:8928308"
```

---

## ⏰ Cron Expressions

```csharp
CronExpressions.EveryMinute         // "* * * * *"
CronExpressions.Every5Minutes       // "*/5 * * * *"
CronExpressions.Every15Minutes      // "*/15 * * * *"
CronExpressions.Hourly              // "0 * * * *"
CronExpressions.Daily               // "0 0 * * *"
CronExpressions.DailyAt2AM          // "0 2 * * *"
CronExpressions.Weekly              // "0 0 * * 0"
CronExpressions.Monthly             // "0 0 1 * *"
CronExpressions.Weekdays9AM         // "0 9 * * 1-5"

// Custom
CronExpressions.DailyAtHour(6)      // "0 6 * * *"
CronExpressions.EveryNMinutes(10)   // "*/10 * * * *"
```

---

## 🕐 Cache Expiration Times

```csharp
CacheExpiration.VeryShort           // 5 minutes
CacheExpiration.Short               // 15 minutes
CacheExpiration.Medium              // 1 hour
CacheExpiration.Long                // 4 hours
CacheExpiration.VeryLong            // 24 hours
CacheExpiration.Permanent           // 365 days
```

---

## 🎯 Program.cs Checklist

```csharp
// ✅ 1. Logging
builder.Host.UseSerilog((context, services, config) =>
    SerilogConfiguration.ConfigureBahyWayLogging(config, "AppName", env));

// ✅ 2. Correlation ID
builder.Services.AddSingleton<ICorrelationIdService, CorrelationIdService>();

// ✅ 3. Application Logger
builder.Services.AddScoped(typeof(IApplicationLogger<>), typeof(ApplicationLogger<>));

// ✅ 4. Current User (for audit)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ✅ 5. Database + Audit
builder.Services.AddDbContext<MyDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
    options.AddInterceptors(new AuditInterceptor(
        sp.GetRequiredService<ICurrentUserService>(),
        sp.GetRequiredService<IApplicationLogger<AuditInterceptor>>()));
});

// ✅ 6. Caching
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ✅ 7. Background Jobs
builder.Services.ConfigureBahyWayHangfire(connectionString, "AppName");

// ✅ 8. Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddRedis(redisConnection);

// ✅ 9. File Storage (if needed)
builder.Services.AddSingleton(new FileStorageOptions { BasePath = "/var/app/storage" });
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// ✅ 10. File Watcher (if needed)
builder.Services.AddSingleton(new FileWatcherOptions { WatchPath = "/var/app/incoming" });
builder.Services.AddSingleton<IFileWatcherService, FileWatcherService>();
```

---

## 🔧 Middleware Checklist

```csharp
// ✅ 1. Correlation ID
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? Guid.NewGuid().ToString("N");
    var service = context.RequestServices.GetRequiredService<ICorrelationIdService>();
    service.SetCorrelationId(correlationId);
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    await next();
});

// ✅ 2. Request Logging
app.UseSerilogRequestLogging();

// ✅ 3. Health Checks
app.MapHealthChecks("/health");

// ✅ 4. Hangfire Dashboard
app.MapHangfireDashboard("/hangfire");
```

---

## 🎨 File Watcher Configuration

```csharp
new FileWatcherOptions
{
    WatchPath = "/var/app/incoming",
    Filter = "*.zip",                           // File pattern
    IncludeSubdirectories = false,
    MinimumFileSizeBytes = 1024,                // 1KB minimum
    MaximumFileSizeBytes = 5L * 1024 * 1024 * 1024,  // 5GB maximum
    StabilizationDelay = TimeSpan.FromSeconds(10),   // Wait after last write
    ProcessExistingFiles = true,                     // Process on startup
    ChangeTypes = WatcherChangeTypes.Created    // Only watch for new files
}
```

---

## 📊 Connection Strings

### Development (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=myapp_dev;Username=dev;Password=dev",
    "Redis": "localhost:6379"
  }
}
```

### Production (appsettings.Production.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.prod.com;Database=myapp;Username=prod_user;Password=${DB_PASSWORD}",
    "Redis": "redis.prod.com:6379,password=${REDIS_PASSWORD},ssl=true"
  }
}
```

---

## 🐳 Docker Commands

```bash
# Start development infrastructure
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all
docker-compose down

# Reset everything
docker-compose down -v
```

---

## 📝 Common Patterns

### Repository with Caching

```csharp
public async Task<Alarm> GetByIdAsync(int id)
{
    return await _cache.GetOrCreateAsync(
        CacheKeys.Alarms.ById(id),
        async () =>
        {
            var alarm = await _dbContext.Alarms.FindAsync(id);
            return alarm;
        },
        CacheExpiration.Medium);
}
```

### Background Job Class

```csharp
public class ProcessAlarmJob : BaseBackgroundJob
{
    private readonly IAlarmService _alarmService;
    
    public ProcessAlarmJob(
        IApplicationLogger<ProcessAlarmJob> logger,
        IAlarmService alarmService) : base(logger)
    {
        _alarmService = alarmService;
    }
    
    protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
    {
        await _alarmService.ProcessPendingAlarmsAsync(cancellationToken);
    }
}
```

### File Processing Pipeline

```csharp
public class FileProcessingService
{
    public async Task InitializeAsync()
    {
        _fileWatcher.FileDetected += OnFileDetected;
        await _fileWatcher.StartAsync();
    }
    
    private void OnFileDetected(object sender, FileWatcherEventArgs e)
    {
        _jobs.Enqueue<ProcessFileJob>(job => job.ProcessAsync(e.FilePath));
    }
}

public class ProcessFileJob : BaseBackgroundJob
{
    protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
    {
        // 1. Extract
        // 2. Validate
        // 3. Transform
        // 4. Load
        // 5. Archive
    }
}
```

---

## 🚨 Troubleshooting

### Issue: Background jobs not processing
```bash
# Check Hangfire dashboard
http://localhost:5000/hangfire

# Check database connection
# Check Hangfire schema exists
SELECT * FROM hangfire.job;
```

### Issue: Cache not working
```bash
# Test Redis connection
redis-cli ping

# Check connection string
# Verify IConnectionMultiplexer is registered
```

### Issue: Logs not appearing
```bash
# Check Serilog configuration
# Verify log file permissions
# Check Seq connection: http://localhost:5341
```

---

## 💡 Tips & Best Practices

1. **Always use correlation IDs** - Makes debugging 10x easier
2. **Cache aggressively** - Reduces DB load by 50-90%
3. **Use background jobs** - Keep API responses fast (<500ms)
4. **Log structured data** - Use properties, not string interpolation
5. **Implement health checks** - Essential for production
6. **Use soft deletes** - Easier recovery from mistakes
7. **Monitor Hangfire** - Watch for failing jobs
8. **Set cache expiration** - Don't cache forever
9. **Use file watcher** - Don't poll file systems
10. **Add retry policies** - Network failures happen

---

## 📞 Need Help?

1. Check **USAGE_GUIDE.md** for detailed examples
2. Review **examples/** folder for real implementations
3. Check **IMPLEMENTATION_ROADMAP.md** for phased approach
4. Reference **NUGET_PACKAGES.md** for dependencies

---

**Keep this cheat sheet handy! 📌**
