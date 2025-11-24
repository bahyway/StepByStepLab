using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;

namespace SharedKernel.Infrastructure.FileWatcher;

/// <summary>
/// Background service that monitors directories for new files and triggers processing
/// Handles large files, zip extraction, file locking, and retry logic
/// Critical for ETLway, HireWay, and other file-based processing systems
/// </summary>
public class FileWatcherService : BackgroundService
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly FileWatcherConfiguration _configuration;
    private readonly IFileProcessor _fileProcessor;
    private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers;
    private readonly ConcurrentDictionary<string, DateTime> _processingFiles;
    private readonly SemaphoreSlim _processingLock;

    public FileWatcherService(
        ILogger<FileWatcherService> logger,
        IOptions<FileWatcherConfiguration> configuration,
        IFileProcessor fileProcessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
        
        _watchers = new ConcurrentDictionary<string, FileSystemWatcher>();
        _processingFiles = new ConcurrentDictionary<string, DateTime>();
        _processingLock = new SemaphoreSlim(_configuration.MaxConcurrentProcessing);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileWatcher service starting with {WatchCount} watch directories", 
            _configuration.WatchDirectories.Count);

        // Initialize watchers for each configured directory
        foreach (var watchConfig in _configuration.WatchDirectories)
        {
            InitializeWatcher(watchConfig);
        }

        // Keep service running
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("FileWatcher service stopping...");
        }
        finally
        {
            // Cleanup watchers
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
        }
    }

    private void InitializeWatcher(WatchDirectoryConfiguration config)
    {
        try
        {
            // Ensure directory exists
            if (!Directory.Exists(config.Path))
            {
                Directory.CreateDirectory(config.Path);
                _logger.LogInformation("Created watch directory: {Path}", config.Path);
            }

            var watcher = new FileSystemWatcher(config.Path)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                Filter = config.FilePattern,
                IncludeSubdirectories = config.IncludeSubdirectories,
                EnableRaisingEvents = true
            };

            // Subscribe to events
            watcher.Created += async (sender, e) => await OnFileCreated(e.FullPath, config);
            watcher.Changed += async (sender, e) => await OnFileChanged(e.FullPath, config);
            watcher.Error += OnError;

            _watchers.TryAdd(config.Name, watcher);

            _logger.LogInformation(
                "FileWatcher initialized for {Name}: Path={Path}, Pattern={Pattern}",
                config.Name, config.Path, config.FilePattern);

            // Process existing files if configured
            if (config.ProcessExistingFiles)
            {
                ProcessExistingFiles(config);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize FileWatcher for {Name}: {Path}", 
                config.Name, config.Path);
        }
    }

    private void ProcessExistingFiles(WatchDirectoryConfiguration config)
    {
        try
        {
            var searchOption = config.IncludeSubdirectories 
                ? SearchOption.AllDirectories 
                : SearchOption.TopDirectoryOnly;

            var existingFiles = Directory.GetFiles(config.Path, config.FilePattern, searchOption);

            _logger.LogInformation("Found {Count} existing files in {Name}", 
                existingFiles.Length, config.Name);

            foreach (var file in existingFiles)
            {
                _ = Task.Run(() => OnFileCreated(file, config));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process existing files in {Name}", config.Name);
        }
    }

    private async Task OnFileCreated(string filePath, WatchDirectoryConfiguration config)
    {
        _logger.LogInformation("File detected: {FilePath}", filePath);

        // Wait for file to be completely written
        if (!await WaitForFileReady(filePath, config.FileReadyTimeout))
        {
            _logger.LogWarning("File not ready after timeout: {FilePath}", filePath);
            await MoveToError(filePath, config, "File not ready after timeout");
            return;
        }

        // Check if already processing
        if (!_processingFiles.TryAdd(filePath, DateTime.UtcNow))
        {
            _logger.LogDebug("File already being processed: {FilePath}", filePath);
            return;
        }

        try
        {
            await _processingLock.WaitAsync();

            _logger.LogInformation("Processing file: {FilePath}", filePath);

            // Get file info
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;

            _logger.LogInformation("File size: {SizeBytes} bytes ({SizeMB} MB)", 
                fileSize, fileSize / (1024.0 * 1024.0));

            // Check if file should be unzipped
            bool isZipFile = Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase);
            
            if (isZipFile && config.AutoExtractZip)
            {
                await ProcessZipFile(filePath, config);
            }
            else
            {
                await ProcessFile(filePath, config);
            }

            // Move to processed folder if configured
            if (!string.IsNullOrWhiteSpace(config.ProcessedPath))
            {
                await MoveToProcessed(filePath, config);
            }
            else if (config.DeleteAfterProcessing)
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted processed file: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file: {FilePath}", filePath);
            await MoveToError(filePath, config, ex.Message);
        }
        finally
        {
            _processingFiles.TryRemove(filePath, out _);
            _processingLock.Release();
        }
    }

    private async Task OnFileChanged(string filePath, WatchDirectoryConfiguration config)
    {
        // Only process if not already being processed
        if (!_processingFiles.ContainsKey(filePath))
        {
            await OnFileCreated(filePath, config);
        }
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        var exception = e.GetException();
        _logger.LogError(exception, "FileWatcher error occurred");
    }

    private async Task<bool> WaitForFileReady(string filePath, int timeoutSeconds)
    {
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                // Try to open file exclusively
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                
                // If we get here, file is not locked
                _logger.LogDebug("File is ready: {FilePath}", filePath);
                return true;
            }
            catch (IOException)
            {
                // File is still locked, wait and retry
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error checking file readiness: {FilePath}", filePath);
                return false;
            }
        }

        return false;
    }

    private async Task ProcessFile(string filePath, WatchDirectoryConfiguration config)
    {
        var fileInfo = new FileInfo(filePath);
        
        // Calculate hash for file integrity
        string fileHash = string.Empty;
        if (config.CalculateHash)
        {
            fileHash = await CalculateFileHash(filePath);
            _logger.LogInformation("File hash: {Hash}", fileHash);
        }

        // Create file metadata
        var metadata = new FileMetadata
        {
            FilePath = filePath,
            FileName = fileInfo.Name,
            FileSize = fileInfo.Length,
            FileHash = fileHash,
            CreatedAt = fileInfo.CreationTimeUtc,
            WatcherName = config.Name,
            ProcessedAt = DateTime.UtcNow
        };

        // Delegate to file processor
        await _fileProcessor.ProcessFileAsync(metadata, config);
    }

    private async Task ProcessZipFile(string zipPath, WatchDirectoryConfiguration config)
    {
        _logger.LogInformation("Extracting zip file: {ZipPath}", zipPath);

        var extractPath = Path.Combine(
            config.ExtractPath ?? Path.GetDirectoryName(zipPath)!,
            Path.GetFileNameWithoutExtension(zipPath));

        try
        {
            // Create extraction directory
            Directory.CreateDirectory(extractPath);

            // Extract zip
            ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);

            _logger.LogInformation("Extracted zip to: {ExtractPath}", extractPath);

            // Process each extracted file
            var extractedFiles = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories);
            
            _logger.LogInformation("Processing {Count} extracted files", extractedFiles.Length);

            foreach (var extractedFile in extractedFiles)
            {
                await ProcessFile(extractedFile, config);
            }

            // Clean up extraction directory if configured
            if (config.DeleteExtractedFiles)
            {
                Directory.Delete(extractPath, recursive: true);
                _logger.LogInformation("Deleted extraction directory: {ExtractPath}", extractPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing zip file: {ZipPath}", zipPath);
            throw;
        }
    }

    private async Task<string> CalculateFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private async Task MoveToProcessed(string filePath, WatchDirectoryConfiguration config)
    {
        try
        {
            if (!Directory.Exists(config.ProcessedPath))
            {
                Directory.CreateDirectory(config.ProcessedPath!);
            }

            var fileName = Path.GetFileName(filePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var newFileName = $"{timestamp}_{fileName}";
            var newPath = Path.Combine(config.ProcessedPath!, newFileName);

            File.Move(filePath, newPath);
            
            _logger.LogInformation("Moved file to processed: {NewPath}", newPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move file to processed: {FilePath}", filePath);
        }
    }

    private async Task MoveToError(string filePath, WatchDirectoryConfiguration config, string errorMessage)
    {
        try
        {
            var errorPath = config.ErrorPath ?? Path.Combine(config.Path, "error");
            
            if (!Directory.Exists(errorPath))
            {
                Directory.CreateDirectory(errorPath);
            }

            var fileName = Path.GetFileName(filePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var newFileName = $"{timestamp}_{fileName}";
            var newPath = Path.Combine(errorPath, newFileName);

            File.Move(filePath, newPath);

            // Write error log
            var errorLogPath = Path.ChangeExtension(newPath, ".error.txt");
            await File.WriteAllTextAsync(errorLogPath, $"Error: {errorMessage}\nTimestamp: {timestamp}");

            _logger.LogWarning("Moved file to error folder: {NewPath}", newPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move file to error folder: {FilePath}", filePath);
        }
    }
}

/// <summary>
/// Interface for file processing implementation
/// Implement this in your application layer
/// </summary>
public interface IFileProcessor
{
    Task ProcessFileAsync(FileMetadata metadata, WatchDirectoryConfiguration config);
}

/// <summary>
/// File metadata captured during processing
/// </summary>
public class FileMetadata
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string WatcherName { get; set; } = string.Empty;
}
