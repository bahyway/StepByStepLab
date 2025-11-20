namespace SharedKernel.Infrastructure.FileWatcher;

/// <summary>
/// Configuration for FileWatcher service
/// </summary>
public class FileWatcherConfiguration
{
    /// <summary>
    /// List of directories to watch
    /// </summary>
    public List<WatchDirectoryConfiguration> WatchDirectories { get; set; } = new();

    /// <summary>
    /// Maximum number of files to process concurrently
    /// </summary>
    public int MaxConcurrentProcessing { get; set; } = 3;

    /// <summary>
    /// Global timeout for file processing in seconds (0 = no timeout)
    /// </summary>
    public int GlobalProcessingTimeout { get; set; } = 0;
}

/// <summary>
/// Configuration for a specific watch directory
/// </summary>
public class WatchDirectoryConfiguration
{
    /// <summary>
    /// Unique name for this watcher
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Directory path to watch
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// File pattern to watch (e.g., "*.csv", "*.zip", "*.*")
    /// </summary>
    public string FilePattern { get; set; } = "*.*";

    /// <summary>
    /// Whether to include subdirectories
    /// </summary>
    public bool IncludeSubdirectories { get; set; } = false;

    /// <summary>
    /// Whether to process files that already exist when service starts
    /// </summary>
    public bool ProcessExistingFiles { get; set; } = true;

    /// <summary>
    /// Timeout in seconds to wait for file to be ready (not locked)
    /// </summary>
    public int FileReadyTimeout { get; set; } = 60;

    /// <summary>
    /// Whether to automatically extract zip files
    /// </summary>
    public bool AutoExtractZip { get; set; } = true;

    /// <summary>
    /// Path where to extract zip files (null = same directory as zip)
    /// </summary>
    public string? ExtractPath { get; set; }

    /// <summary>
    /// Whether to delete extracted files after processing
    /// </summary>
    public bool DeleteExtractedFiles { get; set; } = true;

    /// <summary>
    /// Path to move processed files (null = don't move)
    /// </summary>
    public string? ProcessedPath { get; set; }

    /// <summary>
    /// Path to move files that failed processing (null = auto-create "error" subfolder)
    /// </summary>
    public string? ErrorPath { get; set; }

    /// <summary>
    /// Whether to delete file after successful processing (ignored if ProcessedPath is set)
    /// </summary>
    public bool DeleteAfterProcessing { get; set; } = false;

    /// <summary>
    /// Whether to calculate file hash (SHA256)
    /// </summary>
    public bool CalculateHash { get; set; } = true;

    /// <summary>
    /// Custom metadata for this watcher (application-specific)
    /// </summary>
    public Dictionary<string, string> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Example configurations for BahyWay applications
/// </summary>
public static class FileWatcherConfigurationExamples
{
    /// <summary>
    /// ETLway configuration for data imports
    /// </summary>
    public static FileWatcherConfiguration CreateETLwayConfiguration()
    {
        return new FileWatcherConfiguration
        {
            MaxConcurrentProcessing = 5,
            WatchDirectories = new List<WatchDirectoryConfiguration>
            {
                // CSV imports
                new WatchDirectoryConfiguration
                {
                    Name = "ETL_CSV_Import",
                    Path = "/data/imports/csv",
                    FilePattern = "*.csv",
                    ProcessExistingFiles = true,
                    ProcessedPath = "/data/imports/csv/processed",
                    ErrorPath = "/data/imports/csv/error",
                    CalculateHash = true,
                    CustomMetadata = new Dictionary<string, string>
                    {
                        ["DataSource"] = "External",
                        ["ImportType"] = "CSV"
                    }
                },
                // Zip file imports
                new WatchDirectoryConfiguration
                {
                    Name = "ETL_ZIP_Import",
                    Path = "/data/imports/zip",
                    FilePattern = "*.zip",
                    AutoExtractZip = true,
                    ExtractPath = "/data/imports/zip/extracted",
                    DeleteExtractedFiles = true,
                    ProcessedPath = "/data/imports/zip/processed",
                    FileReadyTimeout = 300, // 5 minutes for large zips
                    CustomMetadata = new Dictionary<string, string>
                    {
                        ["DataSource"] = "External",
                        ["ImportType"] = "Batch"
                    }
                },
                // Excel imports
                new WatchDirectoryConfiguration
                {
                    Name = "ETL_Excel_Import",
                    Path = "/data/imports/excel",
                    FilePattern = "*.xlsx",
                    ProcessedPath = "/data/imports/excel/processed",
                    ErrorPath = "/data/imports/excel/error"
                }
            }
        };
    }

    /// <summary>
    /// HireWay configuration for resume processing
    /// </summary>
    public static FileWatcherConfiguration CreateHireWayConfiguration()
    {
        return new FileWatcherConfiguration
        {
            MaxConcurrentProcessing = 10,
            WatchDirectories = new List<WatchDirectoryConfiguration>
            {
                // Resume uploads
                new WatchDirectoryConfiguration
                {
                    Name = "Resume_Uploads",
                    Path = "/data/resumes/inbox",
                    FilePattern = "*.pdf",
                    ProcessExistingFiles = true,
                    ProcessedPath = "/data/resumes/processed",
                    ErrorPath = "/data/resumes/error",
                    CalculateHash = true,
                    CustomMetadata = new Dictionary<string, string>
                    {
                        ["DocumentType"] = "Resume"
                    }
                },
                // Batch resume imports (zip files)
                new WatchDirectoryConfiguration
                {
                    Name = "Resume_Batch",
                    Path = "/data/resumes/batch",
                    FilePattern = "*.zip",
                    AutoExtractZip = true,
                    ExtractPath = "/data/resumes/batch/extracted",
                    DeleteExtractedFiles = true,
                    ProcessedPath = "/data/resumes/batch/processed",
                    CustomMetadata = new Dictionary<string, string>
                    {
                        ["DocumentType"] = "Resume",
                        ["ProcessType"] = "Batch"
                    }
                }
            }
        };
    }

    /// <summary>
    /// SSISight configuration for SSIS package imports
    /// </summary>
    public static FileWatcherConfiguration CreateSSISightConfiguration()
    {
        return new FileWatcherConfiguration
        {
            MaxConcurrentProcessing = 3,
            WatchDirectories = new List<WatchDirectoryConfiguration>
            {
                // SSIS package imports
                new WatchDirectoryConfiguration
                {
                    Name = "SSIS_Packages",
                    Path = "/data/ssis/inbox",
                    FilePattern = "*.dtsx",
                    ProcessExistingFiles = true,
                    ProcessedPath = "/data/ssis/processed",
                    ErrorPath = "/data/ssis/error",
                    CalculateHash = true,
                    CustomMetadata = new Dictionary<string, string>
                    {
                        ["PackageType"] = "SSIS"
                    }
                }
            }
        };
    }

    /// <summary>
    /// AlarmInsight configuration for historical alarm data
    /// </summary>
    public static FileWatcherConfiguration CreateAlarmInsightConfiguration()
    {
        return new FileWatcherConfiguration
        {
            MaxConcurrentProcessing = 5,
            WatchDirectories = new List<WatchDirectoryConfiguration>
            {
                // Historical alarm data
                new WatchDirectoryConfiguration
                {
                    Name = "Alarm_Historical",
                    Path = "/data/alarms/inbox",
                    FilePattern = "*.csv",
                    ProcessExistingFiles = true,
                    ProcessedPath = "/data/alarms/processed",
                    ErrorPath = "/data/alarms/error",
                    CustomMetadata = new Dictionary<string, string>
                    {
                        ["DataType"] = "Historical",
                        ["Source"] = "SCADA"
                    }
                }
            }
        };
    }

    /// <summary>
    /// NajafCemetery configuration for document imports
    /// </summary>
    public static FileWatcherConfiguration CreateNajafCemeteryConfiguration()
    {
        return new FileWatcherConfiguration
        {
            MaxConcurrentProcessing = 3,
            WatchDirectories = new List<WatchDirectoryConfiguration>
            {
                // Cemetery photos
                new WatchDirectoryConfiguration
                {
                    Name = "Cemetery_Photos",
                    Path = "/data/cemetery/photos/inbox",
                    FilePattern = "*.jpg",
                    ProcessedPath = "/data/cemetery/photos/processed",
                    ErrorPath = "/data/cemetery/photos/error",
                    CalculateHash = true
                },
                // Cemetery documents
                new WatchDirectoryConfiguration
                {
                    Name = "Cemetery_Documents",
                    Path = "/data/cemetery/documents/inbox",
                    FilePattern = "*.pdf",
                    ProcessedPath = "/data/cemetery/documents/processed",
                    ErrorPath = "/data/cemetery/documents/error"
                }
            }
        };
    }
}
