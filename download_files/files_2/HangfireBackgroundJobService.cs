using System.Linq.Expressions;
using Hangfire;

namespace BahyWay.SharedKernel.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire-based implementation of background job service.
/// </summary>
public class HangfireBackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IApplicationLogger<HangfireBackgroundJobService> _logger;

    public HangfireBackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        IApplicationLogger<HangfireBackgroundJobService> logger)
    {
        _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
        _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Enqueue(Expression<Action> methodCall)
    {
        var jobId = _backgroundJobClient.Enqueue(methodCall);
        _logger.LogDebug("Enqueued job: {JobId}", jobId);
        return jobId;
    }

    public string Enqueue(Expression<Func<Task>> methodCall)
    {
        var jobId = _backgroundJobClient.Enqueue(methodCall);
        _logger.LogDebug("Enqueued async job: {JobId}", jobId);
        return jobId;
    }

    public string Schedule(Expression<Action> methodCall, TimeSpan delay)
    {
        var jobId = _backgroundJobClient.Schedule(methodCall, delay);
        _logger.LogDebug("Scheduled job: {JobId} to run in {Delay}", jobId, delay);
        return jobId;
    }

    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        var jobId = _backgroundJobClient.Schedule(methodCall, delay);
        _logger.LogDebug("Scheduled async job: {JobId} to run in {Delay}", jobId, delay);
        return jobId;
    }

    public string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt)
    {
        var jobId = _backgroundJobClient.Schedule(methodCall, enqueueAt);
        _logger.LogDebug("Scheduled job: {JobId} to run at {Time}", jobId, enqueueAt);
        return jobId;
    }

    public void AddOrUpdateRecurringJob(string jobId, Expression<Action> methodCall, string cronExpression)
    {
        _recurringJobManager.AddOrUpdate(jobId, methodCall, cronExpression, TimeZoneInfo.Utc);
        _logger.LogInformation("Added/Updated recurring job: {JobId} with schedule: {Cron}", jobId, cronExpression);
    }

    public void AddOrUpdateRecurringJob(string jobId, Expression<Func<Task>> methodCall, string cronExpression)
    {
        _recurringJobManager.AddOrUpdate(jobId, methodCall, cronExpression, TimeZoneInfo.Utc);
        _logger.LogInformation("Added/Updated recurring async job: {JobId} with schedule: {Cron}", jobId, cronExpression);
    }

    public void RemoveRecurringJob(string jobId)
    {
        _recurringJobManager.RemoveIfExists(jobId);
        _logger.LogInformation("Removed recurring job: {JobId}", jobId);
    }

    public string ContinueWith(string parentJobId, Expression<Action> methodCall)
    {
        var jobId = _backgroundJobClient.ContinueJobWith(parentJobId, methodCall);
        _logger.LogDebug("Created continuation job: {JobId} after {ParentJobId}", jobId, parentJobId);
        return jobId;
    }

    public bool Delete(string jobId)
    {
        var result = _backgroundJobClient.Delete(jobId);
        _logger.LogDebug("Deleted job: {JobId}, Success: {Success}", jobId, result);
        return result;
    }
}

/// <summary>
/// Configuration helper for setting up Hangfire with PostgreSQL.
/// </summary>
public static class HangfireConfiguration
{
    /// <summary>
    /// Configures Hangfire with PostgreSQL storage and recommended settings.
    /// </summary>
    public static void ConfigureBahyWayHangfire(
        this IServiceCollection services,
        string connectionString,
        string applicationName)
    {
        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(connectionString, new Hangfire.PostgreSql.PostgreSqlStorageOptions
                {
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    DashboardJobListLimit = 50000,
                    TransactionTimeout = TimeSpan.FromMinutes(1),
                    SchemaName = $"hangfire_{applicationName.ToLower()}"
                })
                .UseFilter(new AutomaticRetryAttribute
                {
                    Attempts = 3,
                    DelaysInSeconds = new[] { 60, 300, 900 } // 1 min, 5 min, 15 min
                });
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.ServerName = $"{applicationName}-{Environment.MachineName}";
            options.Queues = new[] { "critical", "default", "background" };
        });

        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();
    }
}
