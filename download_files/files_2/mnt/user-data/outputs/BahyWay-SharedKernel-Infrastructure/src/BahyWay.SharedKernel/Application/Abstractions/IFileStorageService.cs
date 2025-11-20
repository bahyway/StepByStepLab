namespace BahyWay.SharedKernel.Application.Abstractions;

/// <summary>
/// Abstraction for file storage operations.
/// Supports local file system, Azure Blob, AWS S3, etc.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to storage.
    /// </summary>
    /// <param name="stream">File stream to upload</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="folder">Optional folder/container path</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>URL or path to the uploaded file</returns>
    Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string folder = null,
        string contentType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from storage.
    /// </summary>
    Task<Stream> DownloadAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata (size, modified date, etc.).
    /// </summary>
    Task<FileMetadata> GetMetadataAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in a folder.
    /// </summary>
    Task<List<FileMetadata>> ListFilesAsync(string folder = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file to a new location.
    /// </summary>
    Task<string> CopyAsync(
        string sourceFilePath,
        string destinationFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file to a new location.
    /// </summary>
    Task<string> MoveAsync(
        string sourceFilePath,
        string destinationFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a temporary URL for accessing a file (for cloud storage).
    /// </summary>
    Task<string> GetTemporaryUrlAsync(
        string filePath,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// File metadata information.
/// </summary>
public class FileMetadata
{
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public long SizeBytes { get; set; }
    public string ContentType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Options for configuring file storage.
/// </summary>
public class FileStorageOptions
{
    public FileStorageProvider Provider { get; set; } = FileStorageProvider.Local;
    public string BasePath { get; set; }
    public string ConnectionString { get; set; }
    public string ContainerName { get; set; }
    public bool AllowPublicAccess { get; set; } = false;
}

/// <summary>
/// Supported file storage providers.
/// </summary>
public enum FileStorageProvider
{
    Local,
    AzureBlob,
    AwsS3
}

/// <summary>
/// Common file path helpers.
/// </summary>
public static class FilePathHelper
{
    /// <summary>
    /// Sanitizes a file name to be safe for storage.
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars));
        return sanitized;
    }

    /// <summary>
    /// Generates a unique file name with timestamp.
    /// Example: "document_20241118_143052_abc123.pdf"
    /// </summary>
    public static string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 6);
        
        return $"{SanitizeFileName(nameWithoutExtension)}_{timestamp}_{uniqueId}{extension}";
    }

    /// <summary>
    /// Combines path parts with forward slashes (cloud-compatible).
    /// </summary>
    public static string CombinePath(params string[] parts)
    {
        return string.Join("/", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    /// <summary>
    /// Gets content type from file extension.
    /// </summary>
    public static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".csv" => "text/csv",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            _ => "application/octet-stream"
        };
    }
}
