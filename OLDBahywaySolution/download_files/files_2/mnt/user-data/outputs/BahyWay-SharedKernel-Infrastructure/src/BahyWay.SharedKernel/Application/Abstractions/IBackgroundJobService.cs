using System.Linq.Expressions;

namespace BahyWay.SharedKernel.Application.Abstractions;

/// <summary>
/// Abstraction for background job scheduling and management.
/// Supports fire-and-forget, delayed, recurring, and continuation jobs.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a fire-and-forget job to be executed once, as soon as possible.
    /// </summary>
    string Enqueue(Expression<Action> methodCall);

    /// <summary>
    /// Enqueues a fire-and-forget async job.
    /// </summary>
    string Enqueue(Expression<Func<Task>> methodCall);

    /// <summary>
    /// Schedules a job to be executed after a specified delay.
    /// </summary>
    string Schedule(Expression<Action> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedules an async job to be executed after a specified delay.
    /// </summary>
    string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedules a job to be executed at a specific time.
    /// </summary>
    string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt);

    /// <summary>
    /// Creates or updates a recurring job with a cron expression.
    /// </summary>
    void AddOrUpdateRecurringJob(string jobId, Expression<Action> methodCall, string cronExpression);

    /// <summary>
    /// Creates or updates a recurring async job.
    /// </summary>
    void AddOrUpdateRecurringJob(string jobId, Expression<Func<Task>> methodCall, string cronExpression);

    /// <summary>
    /// Removes a recurring job.
    /// </summary>
    void RemoveRecurringJob(string jobId);

    /// <summary>
    /// Creates a continuation job that will be executed after a parent job completes successfully.
    /// </summary>
    string ContinueWith(string parentJobId, Expression<Action> methodCall);

    /// <summary>
    /// Deletes a job from the queue.
    /// </summary>
    bool Delete(string jobId);
}

/// <summary>
/// Cron expression helpers for common scheduling patterns.
/// </summary>
public static class CronExpressions
{
    /// <summary>Every minute</summary>
    public const string EveryMinute = "* * * * *";
    
    /// <summary>Every 5 minutes</summary>
    public const string Every5Minutes = "*/5 * * * *";
    
    /// <summary>Every 15 minutes</summary>
    public const string Every15Minutes = "*/15 * * * *";
    
    /// <summary>Every 30 minutes</summary>
    public const string Every30Minutes = "*/30 * * * *";
    
    /// <summary>Every hour at minute 0</summary>
    public const string Hourly = "0 * * * *";
    
    /// <summary>Every day at midnight UTC</summary>
    public const string Daily = "0 0 * * *";
    
    /// <summary>Every day at 2 AM UTC</summary>
    public const string DailyAt2AM = "0 2 * * *";
    
    /// <summary>Every Sunday at midnight UTC</summary>
    public const string Weekly = "0 0 * * 0";
    
    /// <summary>First day of every month at midnight UTC</summary>
    public const string Monthly = "0 0 1 * *";
    
    /// <summary>Every weekday (Mon-Fri) at 9 AM UTC</summary>
    public const string Weekdays9AM = "0 9 * * 1-5";

    /// <summary>
    /// Creates a custom daily cron expression for a specific hour (UTC).
    /// </summary>
    public static string DailyAtHour(int hour)
    {
        if (hour < 0 || hour > 23)
            throw new ArgumentOutOfRangeException(nameof(hour), "Hour must be between 0 and 23");
        
        return $"0 {hour} * * *";
    }

    /// <summary>
    /// Creates a custom cron expression for every N minutes.
    /// </summary>
    public static string EveryNMinutes(int minutes)
    {
        if (minutes < 1 || minutes > 59)
            throw new ArgumentOutOfRangeException(nameof(minutes), "Minutes must be between 1 and 59");
        
        return $"*/{minutes} * * * *";
    }
}

/// <summary>
/// Base class for background jobs in BahyWay applications.
/// Provides logging and error handling infrastructure.
/// </summary>
public abstract class BaseBackgroundJob
{
    protected IApplicationLogger<BaseBackgroundJob> Logger { get; }

    protected BaseBackgroundJob(IApplicationLogger<BaseBackgroundJob> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the job with automatic error handling and logging.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var jobName = GetType().Name;
        
        Logger.LogInformation("Starting background job: {JobName}", jobName);
        var startTime = DateTime.UtcNow;

        try
        {
            await ExecuteInternalAsync(cancellationToken);
            
            var duration = DateTime.UtcNow - startTime;
            Logger.LogInformation(
                "Background job {JobName} completed successfully in {Duration}ms",
                jobName,
                duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Logger.LogError(
                ex,
                "Background job {JobName} failed after {Duration}ms",
                jobName,
                duration.TotalMilliseconds);
            
            throw;
        }
    }

    /// <summary>
    /// Implement this method with the actual job logic.
    /// </summary>
    protected abstract Task ExecuteInternalAsync(CancellationToken cancellationToken);
}
