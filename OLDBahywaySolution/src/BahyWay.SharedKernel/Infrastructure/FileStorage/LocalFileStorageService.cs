namespace BahyWay.SharedKernel.Infrastructure.FileStorage;

/// <summary>
/// Local file system implementation of file storage.
/// Suitable for single-server deployments and development.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;
    private readonly IApplicationLogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        FileStorageOptions options,
        IApplicationLogger<LocalFileStorageService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.BasePath))
            throw new ArgumentException("BasePath must be specified for local storage", nameof(options));

        // Ensure base directory exists
        if (!Directory.Exists(_options.BasePath))
        {
            Directory.CreateDirectory(_options.BasePath);
            _logger.LogInformation("Created storage directory: {Path}", _options.BasePath);
        }
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string folder = null,
        string contentType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedFileName = FilePathHelper.SanitizeFileName(fileName);
            var relativePath = string.IsNullOrWhiteSpace(folder)
                ? sanitizedFileName
                : Path.Combine(folder, sanitizedFileName);

            var fullPath = Path.Combine(_options.BasePath, relativePath);
            var directory = Path.GetDirectoryName(fullPath);

            // Ensure directory exists
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Upload file
            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream, cancellationToken);

            _logger.LogInformation("Uploaded file: {FileName} to {Path}", fileName, relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_options.BasePath, filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var memoryStream = new MemoryStream();
            using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            _logger.LogDebug("Downloaded file: {FilePath}", filePath);

            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FilePath}", filePath);
            throw;
        }
    }

    public Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_options.BasePath, filePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted file: {FilePath}", filePath);
            }
            else
            {
                _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            throw;
        }
    }

    public Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_options.BasePath, filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<FileMetadata> GetMetadataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_options.BasePath, filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(fullPath);

            var metadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                SizeBytes = fileInfo.Length,
                ContentType = FilePathHelper.GetContentType(fileInfo.Name),
                CreatedAt = fileInfo.CreationTimeUtc,
                LastModifiedAt = fileInfo.LastWriteTimeUtc
            };

            return Task.FromResult(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for file: {FilePath}", filePath);
            throw;
        }
    }

    public Task<List<FileMetadata>> ListFilesAsync(string folder = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchPath = string.IsNullOrWhiteSpace(folder)
                ? _options.BasePath
                : Path.Combine(_options.BasePath, folder);

            if (!Directory.Exists(searchPath))
            {
                return Task.FromResult(new List<FileMetadata>());
            }

            var files = Directory.GetFiles(searchPath, "*.*", SearchOption.TopDirectoryOnly);
            
            var metadata = files.Select(file =>
            {
                var fileInfo = new FileInfo(file);
                var relativePath = Path.GetRelativePath(_options.BasePath, file);

                return new FileMetadata
                {
                    FilePath = relativePath,
                    FileName = fileInfo.Name,
                    SizeBytes = fileInfo.Length,
                    ContentType = FilePathHelper.GetContentType(fileInfo.Name),
                    CreatedAt = fileInfo.CreationTimeUtc,
                    LastModifiedAt = fileInfo.LastWriteTimeUtc
                };
            }).ToList();

            return Task.FromResult(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files in folder: {Folder}", folder);
            throw;
        }
    }

    public Task<string> CopyAsync(
        string sourceFilePath,
        string destinationFilePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceFull = Path.Combine(_options.BasePath, sourceFilePath);
            var destinationFull = Path.Combine(_options.BasePath, destinationFilePath);

            if (!File.Exists(sourceFull))
            {
                throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
            }

            var destinationDirectory = Path.GetDirectoryName(destinationFull);
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourceFull, destinationFull, overwrite: true);

            _logger.LogInformation("Copied file from {Source} to {Destination}", sourceFilePath, destinationFilePath);

            return Task.FromResult(destinationFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying file from {Source} to {Destination}", sourceFilePath, destinationFilePath);
            throw;
        }
    }

    public Task<string> MoveAsync(
        string sourceFilePath,
        string destinationFilePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceFull = Path.Combine(_options.BasePath, sourceFilePath);
            var destinationFull = Path.Combine(_options.BasePath, destinationFilePath);

            if (!File.Exists(sourceFull))
            {
                throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
            }

            var destinationDirectory = Path.GetDirectoryName(destinationFull);
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Move(sourceFull, destinationFull, overwrite: true);

            _logger.LogInformation("Moved file from {Source} to {Destination}", sourceFilePath, destinationFilePath);

            return Task.FromResult(destinationFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving file from {Source} to {Destination}", sourceFilePath, destinationFilePath);
            throw;
        }
    }

    public Task<string> GetTemporaryUrlAsync(
        string filePath,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        // Local file storage doesn't support temporary URLs
        // Return the file path instead
        _logger.LogWarning("Temporary URLs not supported for local storage, returning file path");
        return Task.FromResult(filePath);
    }
}
