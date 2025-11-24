using BahyWay.SharedKernel.Application.Abstractions;
using BahyWay.SharedKernel.Infrastructure.FileWatcher;

namespace BahyWay.ETLway.Application.Jobs;

/// <summary>
/// Example: ETL File Processing Pipeline using FileWatcher + Background Jobs.
/// This demonstrates how to automatically process uploaded ZIP files in ETLway.
/// </summary>
public class EtlFileProcessingService
{
    private readonly IFileWatcherService _fileWatcher;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IApplicationLogger<EtlFileProcessingService> _logger;

    public EtlFileProcessingService(
        IFileWatcherService fileWatcher,
        IBackgroundJobService backgroundJobService,
        IApplicationLogger<EtlFileProcessingService> logger)
    {
        _fileWatcher = fileWatcher;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the file watcher and subscribes to file detection events.
    /// Call this in your application startup (Program.cs or StartupHostedService).
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Subscribe to file detection events
        _fileWatcher.FileDetected += OnFileDetected;

        // Start watching
        await _fileWatcher.StartAsync(cancellationToken);

        _logger.LogInformation("ETL File Processing Service initialized");
    }

    /// <summary>
    /// Event handler when a new file is detected.
    /// Enqueues a background job to process the file.
    /// </summary>
    private void OnFileDetected(object sender, FileWatcherEventArgs e)
    {
        _logger.LogInformation(
            "New file detected: {FileName} ({Size} bytes)",
            e.FileName,
            e.FileSize);

        // Enqueue background job to process the file
        // This returns immediately and processing happens in the background
        var jobId = _backgroundJobService.Enqueue<EtlFileProcessorJob>(
            job => job.ProcessFileAsync(e.FilePath, CancellationToken.None));

        _logger.LogInformation(
            "Enqueued processing job {JobId} for file: {FileName}",
            jobId,
            e.FileName);
    }
}

/// <summary>
/// Background job that processes uploaded ETL files.
/// </summary>
public class EtlFileProcessorJob : BaseBackgroundJob
{
    private readonly IApplicationLogger<EtlFileProcessorJob> _logger;
    private readonly IEtlService _etlService;

    public EtlFileProcessorJob(
        IApplicationLogger<EtlFileProcessorJob> logger,
        IEtlService etlService)
        : base(logger)
    {
        _logger = logger;
        _etlService = etlService;
    }

    /// <summary>
    /// Processes a single ETL file.
    /// This runs in the background via Hangfire.
    /// </summary>
    public async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken)
    {
        await ExecuteAsync(cancellationToken);
    }

    protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
    {
        // Your ETL processing logic here
        // For example:
        // 1. Extract ZIP file
        // 2. Validate contents
        // 3. Parse data files
        // 4. Transform data
        // 5. Load into database
        // 6. Archive processed file
        
        await _etlService.ProcessAsync(filePath, cancellationToken);
    }
}

/// <summary>
/// Configuration in Program.cs or Startup.
/// </summary>
public static class EtlConfigurationExample
{
    public static void ConfigureEtlFileProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure File Watcher
        var watchPath = configuration["ETL:WatchPath"] ?? "/var/etl/incoming";
        
        services.AddSingleton(new FileWatcherOptions
        {
            WatchPath = watchPath,
            Filter = "*.zip",  // Watch for ZIP files
            IncludeSubdirectories = false,
            MinimumFileSizeBytes = 1024,  // Minimum 1KB
            MaximumFileSizeBytes = 5L * 1024 * 1024 * 1024,  // Maximum 5GB
            StabilizationDelay = TimeSpan.FromSeconds(10),  // Wait 10 seconds after last write
            ProcessExistingFiles = true,  // Process files that exist on startup
            ChangeTypes = WatcherChangeTypes.Created
        });

        services.AddSingleton<IFileWatcherService, FileWatcherService>();
        services.AddScoped<EtlFileProcessingService>();
        services.AddScoped<EtlFileProcessorJob>();
    }
}

/// <summary>
/// Hosted service to start file watching on application startup.
/// </summary>
public class EtlFileWatcherHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IApplicationLogger<EtlFileWatcherHostedService> _logger;

    public EtlFileWatcherHostedService(
        IServiceProvider serviceProvider,
        IApplicationLogger<EtlFileWatcherHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<EtlFileProcessingService>();
        
        await service.InitializeAsync(cancellationToken);
        
        _logger.LogInformation("ETL File Watcher Hosted Service started");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ETL File Watcher Hosted Service stopping");
        return Task.CompletedTask;
    }
}

// Placeholder interface
public interface IEtlService
{
    Task ProcessAsync(string filePath, CancellationToken cancellationToken);
}

/// <summary>
/// Example Program.cs configuration
/// </summary>
public class ProgramExample
{
    public static void Example(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Hangfire for background jobs
        builder.Services.ConfigureBahyWayHangfire(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            "ETLway");

        // Configure ETL file processing
        builder.Services.ConfigureEtlFileProcessing(builder.Configuration);

        // Add hosted service to start file watcher
        builder.Services.AddHostedService<EtlFileWatcherHostedService>();

        var app = builder.Build();

        // ... rest of application configuration
    }
}
