using MediatR;
using BahyWay.SharedKernel.Domain.Primitives;
using BahyWay.SharedKernel.Application.Abstractions;
using AlarmInsight.Domain.Aggregates;
using AlarmInsight.Domain.ValueObjects;
using AlarmInsight.Application.Abstractions;

namespace AlarmInsight.Application.Alarms.Commands.CreateAlarm;

/// <summary>
/// Handler for CreateAlarmCommand.
/// DEMONSTRATES: How to use ALL SharedKernel abstractions together!
/// </summary>
public sealed class CreateAlarmCommandHandler : IRequestHandler<CreateAlarmCommand, Result<int>>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationLogger<CreateAlarmCommandHandler> _logger; // ← SharedKernel
    private readonly ICacheService _cache; // ← SharedKernel
    private readonly IBackgroundJobService _backgroundJobs; // ← SharedKernel

    public CreateAlarmCommandHandler(
        IAlarmRepository alarmRepository,
        IUnitOfWork unitOfWork,
        IApplicationLogger<CreateAlarmCommandHandler> logger,
        ICacheService cache,
        IBackgroundJobService backgroundJobs)
    {
        _alarmRepository = alarmRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
        _backgroundJobs = backgroundJobs;
    }

    public async Task<Result<int>> Handle(
        CreateAlarmCommand request,
        CancellationToken cancellationToken)
    {
        // 1. LOG: Start processing (uses IApplicationLogger from SharedKernel)
        _logger.LogInformation(
            "Creating alarm from source: {Source}, Severity: {Severity}",
            request.Source,
            request.SeverityValue);

        // 2. CREATE VALUE OBJECTS: Location
        var locationResult = Location.Create(
            request.LocationName,
            request.Latitude,
            request.Longitude);

        if (locationResult.IsFailure)
        {
            _logger.LogWarning("Invalid location: {Error}", locationResult.Error.Message);
            return Result.Failure<int>(locationResult.Error);
        }

        // 3. CREATE VALUE OBJECTS: Severity
        var severityResult = AlarmSeverity.FromValue(request.SeverityValue);

        if (severityResult.IsFailure)
        {
            _logger.LogWarning("Invalid severity: {Error}", severityResult.Error.Message);
            return Result.Failure<int>(severityResult.Error);
        }

        // 4. CREATE DOMAIN ENTITY: Using factory method with Result pattern
        var alarmResult = Alarm.Create(
            request.Source,
            request.Description,
            severityResult.Value,
            locationResult.Value);

        if (alarmResult.IsFailure)
        {
            _logger.LogWarning("Failed to create alarm: {Error}", alarmResult.Error.Message);
            return Result.Failure<int>(alarmResult.Error);
        }

        var alarm = alarmResult.Value;

        // 5. SET AUDIT INFO: AuditableEntity from SharedKernel
        // In real app, get current user from ICurrentUserService
        alarm.MarkAsCreated("System");

        // 6. PERSIST: Add to repository
        await _alarmRepository.AddAsync(alarm, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alarm created successfully with ID: {AlarmId}", alarm.Id);

        // 7. INVALIDATE CACHE: Remove cached active alarms (uses ICacheService)
        await _cache.RemoveByPatternAsync(CacheKeys.Alarms.Pattern());
        _logger.LogDebug("Cache invalidated for alarm pattern");

        // 8. ENQUEUE BACKGROUND JOB: Process alarm asynchronously (uses IBackgroundJobService)
        // Note: ProcessAlarmJob will be created in Infrastructure layer
        _backgroundJobs.Enqueue(() => ProcessAlarmNotificationJob(alarm.Id));
        _logger.LogDebug("Background job enqueued for alarm: {AlarmId}", alarm.Id);

        // 9. RETURN SUCCESS: With alarm ID
        return Result.Success(alarm.Id);
    }

    /// <summary>
    /// Background job method signature.
    /// Actual implementation will be in Infrastructure layer.
    /// </summary>
    private static Task ProcessAlarmNotificationJob(int alarmId)
    {
        // This is just a placeholder - actual job implementation goes in Infrastructure
        return Task.CompletedTask;
    }
}
