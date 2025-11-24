namespace BahyWay.SharedKernel.Infrastructure.FileWatcher;

/// <summary>
/// Configuration for file system watcher.
/// </summary>
public class FileWatcherOptions
{
    /// <summary>
    /// Path to watch for file changes.
    /// </summary>
    public string WatchPath { get; set; }

    /// <summary>
    /// File filter (e.g., "*.zip", "*.csv", "*.*").
    /// </summary>
    public string Filter { get; set; } = "*.*";

    /// <summary>
    /// Whether to watch subdirectories.
    /// </summary>
    public bool IncludeSubdirectories { get; set; } = false;

    /// <summary>
    /// Minimum file size in bytes to process (helps avoid processing incomplete uploads).
    /// </summary>
    public long MinimumFileSizeBytes { get; set; } = 0;

    /// <summary>
    /// Time to wait after last write before processing (helps ensure file is completely written).
    /// </summary>
    public TimeSpan StabilizationDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Types of changes to monitor.
    /// </summary>
    public WatcherChangeTypes ChangeTypes { get; set; } = WatcherChangeTypes.Created | WatcherChangeTypes.Changed;

    /// <summary>
    /// Whether to process existing files on startup.
    /// </summary>
    public bool ProcessExistingFiles { get; set; } = true;

    /// <summary>
    /// Maximum file size in bytes to process (prevents processing extremely large files).
    /// Default: 5GB
    /// </summary>
    public long MaximumFileSizeBytes { get; set; } = 5L * 1024 * 1024 * 1024;
}

/// <summary>
/// Event arguments for file watcher events.
/// </summary>
public class FileWatcherEventArgs : EventArgs
{
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public DateTime DetectedAt { get; set; }
    public WatcherChangeTypes ChangeType { get; set; }
}

/// <summary>
/// Interface for file watcher service.
/// </summary>
public interface IFileWatcherService
{
    /// <summary>
    /// Event raised when a file is detected and ready for processing.
    /// </summary>
    event EventHandler<FileWatcherEventArgs> FileDetected;

    /// <summary>
    /// Starts watching the configured path.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops watching.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the watcher is currently running.
    /// </summary>
    bool IsWatching { get; }
}

/// <summary>
/// File system watcher service with stabilization and duplicate detection.
/// Critical for ETLway and file-based processing pipelines.
/// </summary>
public class FileWatcherService : IFileWatcherService, IDisposable
{
    private readonly FileWatcherOptions _options;
    private readonly IApplicationLogger<FileWatcherService> _logger;
    private FileSystemWatcher _watcher;
    private readonly ConcurrentDictionary<string, DateTime> _pendingFiles = new();
    private readonly ConcurrentDictionary<string, bool> _processedFiles = new();
    private readonly Timer _stabilizationTimer;
    private bool _isWatching;

    public event EventHandler<FileWatcherEventArgs> FileDetected;

    public bool IsWatching => _isWatching;

    public FileWatcherService(
        FileWatcherOptions options,
        IApplicationLogger<FileWatcherService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.WatchPath))
            throw new ArgumentException("WatchPath must be specified", nameof(options));

        _stabilizationTimer = new Timer(CheckStabilizedFiles, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isWatching)
        {
            _logger.LogWarning("File watcher is already running");
            return Task.CompletedTask;
        }

        try
        {
            // Ensure watch directory exists
            if (!Directory.Exists(_options.WatchPath))
            {
                Directory.CreateDirectory(_options.WatchPath);
                _logger.LogInformation("Created watch directory: {Path}", _options.WatchPath);
            }

            // Configure file system watcher
            _watcher = new FileSystemWatcher(_options.WatchPath)
            {
                Filter = _options.Filter,
                IncludeSubdirectories = _options.IncludeSubdirectories,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            // Wire up events
            if (_options.ChangeTypes.HasFlag(WatcherChangeTypes.Created))
                _watcher.Created += OnFileChanged;
            
            if (_options.ChangeTypes.HasFlag(WatcherChangeTypes.Changed))
                _watcher.Changed += OnFileChanged;

            _watcher.Error += OnError;

            // Start watching
            _watcher.EnableRaisingEvents = true;
            _isWatching = true;

            // Start stabilization timer
            _stabilizationTimer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            _logger.LogInformation(
                "File watcher started. Path: {Path}, Filter: {Filter}, Subdirectories: {Subdirectories}",
                _options.WatchPath,
                _options.Filter,
                _options.IncludeSubdirectories);

            // Process existing files if configured
            if (_options.ProcessExistingFiles)
            {
                ProcessExistingFiles();
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start file watcher");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isWatching)
            return Task.CompletedTask;

        try
        {
            _stabilizationTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            _isWatching = false;
            _pendingFiles.Clear();

            _logger.LogInformation("File watcher stopped");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping file watcher");
            throw;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Skip directories
            if (Directory.Exists(e.FullPath))
                return;

            // Add to pending files for stabilization
            _pendingFiles.AddOrUpdate(e.FullPath, DateTime.UtcNow, (_, __) => DateTime.UtcNow);

            _logger.LogDebug("File change detected: {Path}, Type: {Type}", e.FullPath, e.ChangeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file change event for {Path}", e.FullPath);
        }
    }

    private void CheckStabilizedFiles(object state)
    {
        var now = DateTime.UtcNow;
        var stabilizedFiles = _pendingFiles
            .Where(kvp => now - kvp.Value >= _options.StabilizationDelay)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var filePath in stabilizedFiles)
        {
            // Remove from pending
            _pendingFiles.TryRemove(filePath, out _);

            // Skip if already processed recently
            if (_processedFiles.ContainsKey(filePath))
            {
                // Clean up old processed files (older than 1 hour)
                var oldFiles = _processedFiles
                    .Where(kvp => now - DateTime.UtcNow > TimeSpan.FromHours(1))
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var oldFile in oldFiles)
                    _processedFiles.TryRemove(oldFile, out _);

                continue;
            }

            // Process the stabilized file
            ProcessFile(filePath);
        }
    }

    private void ProcessFile(string filePath)
    {
        try
        {
            // Ensure file still exists
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("File no longer exists, skipping: {Path}", filePath);
                return;
            }

            var fileInfo = new FileInfo(filePath);

            // Check minimum size
            if (fileInfo.Length < _options.MinimumFileSizeBytes)
            {
                _logger.LogDebug(
                    "File {Path} is too small ({Size} bytes), skipping",
                    filePath,
                    fileInfo.Length);
                return;
            }

            // Check maximum size
            if (fileInfo.Length > _options.MaximumFileSizeBytes)
            {
                _logger.LogWarning(
                    "File {Path} is too large ({Size} bytes), skipping",
                    filePath,
                    fileInfo.Length);
                return;
            }

            // Check if file is locked (being written)
            if (IsFileLocked(fileInfo))
            {
                _logger.LogDebug("File {Path} is locked, will retry", filePath);
                _pendingFiles.TryAdd(filePath, DateTime.UtcNow);
                return;
            }

            // Mark as processed
            _processedFiles.TryAdd(filePath, true);

            // Raise event
            var eventArgs = new FileWatcherEventArgs
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                DetectedAt = DateTime.UtcNow,
                ChangeType = WatcherChangeTypes.Created
            };

            _logger.LogInformation(
                "File ready for processing: {FileName} ({Size} bytes)",
                fileInfo.Name,
                fileInfo.Length);

            FileDetected?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file: {Path}", filePath);
        }
    }

    private void ProcessExistingFiles()
    {
        try
        {
            var searchOption = _options.IncludeSubdirectories 
                ? SearchOption.AllDirectories 
                : SearchOption.TopDirectoryOnly;

            var files = Directory.GetFiles(_options.WatchPath, _options.Filter, searchOption);

            _logger.LogInformation("Found {Count} existing files to process", files.Length);

            foreach (var file in files)
            {
                _pendingFiles.TryAdd(file, DateTime.UtcNow.AddSeconds(-_options.StabilizationDelay.TotalSeconds - 1));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing existing files");
        }
    }

    private bool IsFileLocked(FileInfo file)
    {
        try
        {
            using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "File watcher error occurred");
    }

    public void Dispose()
    {
        _stabilizationTimer?.Dispose();
        _watcher?.Dispose();
    }
}
