namespace BahyWay.SharedKernel.Application.Abstractions;

/// <summary>
/// Abstraction for caching operations supporting both in-memory and distributed caching.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value or creates it using the factory function if not found.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cached value.
    /// </summary>
    Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a cached value.
    /// </summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached value.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cached values matching the pattern.
    /// Pattern format: "user:*" removes all keys starting with "user:"
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in cache.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the expiration time of a cached item.
    /// </summary>
    Task RefreshAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache key builder for consistent key naming across the application.
/// </summary>
public static class CacheKeys
{
    private const string Delimiter = ":";

    /// <summary>
    /// Builds a cache key with consistent formatting.
    /// Example: BuildKey("user", userId, "profile") => "user:123:profile"
    /// </summary>
    public static string BuildKey(params object[] parts)
    {
        return string.Join(Delimiter, parts.Select(p => p?.ToString() ?? "null"));
    }

    /// <summary>
    /// Builds a pattern for cache invalidation.
    /// Example: BuildPattern("user", "*") => "user:*"
    /// </summary>
    public static string BuildPattern(params object[] parts)
    {
        return BuildKey(parts);
    }

    // Common key patterns for BahyWay applications
    public static class Alarms
    {
        public static string ById(int alarmId) => BuildKey("alarm", alarmId);
        public static string ByLocation(string location) => BuildKey("alarm", "location", location);
        public static string AllActive() => BuildKey("alarm", "active");
        public static string Pattern() => BuildPattern("alarm", "*");
    }

    public static class Forecasts
    {
        public static string ById(int forecastId) => BuildKey("forecast", forecastId);
        public static string ByModel(string model) => BuildKey("forecast", "model", model);
        public static string Pattern() => BuildPattern("forecast", "*");
    }

    public static class Candidates
    {
        public static string ById(int candidateId) => BuildKey("candidate", candidateId);
        public static string SearchResults(string query) => BuildKey("candidate", "search", query);
        public static string Pattern() => BuildPattern("candidate", "*");
    }

    public static class GeoSpatial
    {
        public static string ByH3Index(string h3Index) => BuildKey("geo", "h3", h3Index);
        public static string ByCemetery(int cemeteryId) => BuildKey("geo", "cemetery", cemeteryId);
        public static string Pattern() => BuildPattern("geo", "*");
    }
}

/// <summary>
/// Cache expiration constants.
/// </summary>
public static class CacheExpiration
{
    public static readonly TimeSpan VeryShort = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Short = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan Medium = TimeSpan.FromHours(1);
    public static readonly TimeSpan Long = TimeSpan.FromHours(4);
    public static readonly TimeSpan VeryLong = TimeSpan.FromHours(24);
    public static readonly TimeSpan Permanent = TimeSpan.FromDays(365);
}
