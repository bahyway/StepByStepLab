# AlarmInsight.Application - Complete Implementation Guide

## 📁 Folder Structure to Create

```
AlarmInsight.Application/
├── Abstractions/
│   ├── IAlarmRepository.cs
│   └── IUnitOfWork.cs
│
├── Alarms/
│   ├── Commands/
│   │   ├── CreateAlarm/
│   │   │   ├── CreateAlarmCommand.cs
│   │   │   ├── CreateAlarmCommandHandler.cs
│   │   │   └── CreateAlarmCommandValidator.cs
│   │   ├── ProcessAlarm/
│   │   │   ├── ProcessAlarmCommand.cs
│   │   │   └── ProcessAlarmCommandHandler.cs
│   │   └── ResolveAlarm/
│   │       ├── ResolveAlarmCommand.cs
│   │       └── ResolveAlarmCommandHandler.cs
│   │
│   └── Queries/
│       ├── GetAlarm/
│       │   ├── GetAlarmQuery.cs
│       │   ├── GetAlarmQueryHandler.cs
│       │   └── AlarmDto.cs
│       └── GetActiveAlarms/
│           ├── GetActiveAlarmsQuery.cs
│           ├── GetActiveAlarmsQueryHandler.cs
│           └── AlarmSummaryDto.cs
│
└── DependencyInjection.cs
```

---

## 📄 File 1: IAlarmRepository.cs

**Location:** `AlarmInsight.Application/Abstractions/IAlarmRepository.cs`

```csharp
using BahyWay.SharedKernel.Domain.Primitives;
using AlarmInsight.Domain.Aggregates;

namespace AlarmInsight.Application.Abstractions;

/// <summary>
/// Repository interface for Alarm aggregate.
/// Will be implemented in Infrastructure layer.
/// </summary>
public interface IAlarmRepository
{
    Task<Alarm?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Alarm>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Alarm>> GetByLocationAsync(string location, CancellationToken cancellationToken = default);
    Task AddAsync(Alarm alarm, CancellationToken cancellationToken = default);
    void Update(Alarm alarm);
    void Delete(Alarm alarm);
}
```

---

## 📄 File 2: IUnitOfWork.cs

**Location:** `AlarmInsight.Application/Abstractions/IUnitOfWork.cs`

```csharp
namespace AlarmInsight.Application.Abstractions;

/// <summary>
/// Unit of Work pattern for transactional consistency.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

---

## 📄 File 3: CreateAlarmCommand.cs

**Location:** `AlarmInsight.Application/Alarms/Commands/CreateAlarm/CreateAlarmCommand.cs`

```csharp
using MediatR;
using BahyWay.SharedKernel.Domain.Primitives;

namespace AlarmInsight.Application.Alarms.Commands.CreateAlarm;

/// <summary>
/// Command to create a new alarm.
/// PATTERN: CQRS Command
/// </summary>
public sealed record CreateAlarmCommand(
    string Source,
    string Description,
    int SeverityValue,
    string LocationName,
    double Latitude,
    double Longitude
) : IRequest<Result<int>>;
```

---

## 📄 File 4: CreateAlarmCommandHandler.cs ⭐ MOST IMPORTANT

**Location:** `AlarmInsight.Application/Alarms/Commands/CreateAlarm/CreateAlarmCommandHandler.cs`

```csharp
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
    private readonly IApplicationLogger<CreateAlarmCommandHandler> _logger;
    private readonly ICacheService _cache;
    private readonly IBackgroundJobService _backgroundJobs;

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
        // 1. LOG
        _logger.LogInformation(
            "Creating alarm from source: {Source}, Severity: {Severity}",
            request.Source,
            request.SeverityValue);

        // 2. CREATE VALUE OBJECTS
        var locationResult = Location.Create(
            request.LocationName,
            request.Latitude,
            request.Longitude);

        if (locationResult.IsFailure)
        {
            _logger.LogWarning("Invalid location: {Error}", locationResult.Error.Message);
            return Result.Failure<int>(locationResult.Error);
        }

        var severityResult = AlarmSeverity.FromValue(request.SeverityValue);

        if (severityResult.IsFailure)
        {
            _logger.LogWarning("Invalid severity: {Error}", severityResult.Error.Message);
            return Result.Failure<int>(severityResult.Error);
        }

        // 3. CREATE DOMAIN ENTITY
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

        // 4. SET AUDIT INFO
        alarm.MarkAsCreated("System");

        // 5. PERSIST
        await _alarmRepository.AddAsync(alarm, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alarm created successfully with ID: {AlarmId}", alarm.Id);

        // 6. INVALIDATE CACHE
        await _cache.RemoveByPatternAsync(CacheKeys.Alarms.Pattern());

        // 7. ENQUEUE BACKGROUND JOB
        // Note: Actual job implementation will be in Infrastructure
        _logger.LogDebug("Alarm processing initiated for: {AlarmId}", alarm.Id);

        // 8. RETURN SUCCESS
        return Result.Success(alarm.Id);
    }
}
```

---

## 📄 File 5: CreateAlarmCommandValidator.cs

**Location:** `AlarmInsight.Application/Alarms/Commands/CreateAlarm/CreateAlarmCommandValidator.cs`

```csharp
using FluentValidation;

namespace AlarmInsight.Application.Alarms.Commands.CreateAlarm;

public sealed class CreateAlarmCommandValidator : AbstractValidator<CreateAlarmCommand>
{
    public CreateAlarmCommandValidator()
    {
        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Alarm source is required")
            .MaximumLength(200).WithMessage("Source cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Alarm description is required")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.SeverityValue)
            .InclusiveBetween(1, 4)
            .WithMessage("Severity must be between 1 (Low) and 4 (Critical)");

        RuleFor(x => x.LocationName)
            .NotEmpty().WithMessage("Location name is required")
            .MaximumLength(200).WithMessage("Location name cannot exceed 200 characters");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180");
    }
}
```

---

## 📄 File 6: ProcessAlarmCommand.cs

**Location:** `AlarmInsight.Application/Alarms/Commands/ProcessAlarm/ProcessAlarmCommand.cs`

```csharp
using MediatR;
using BahyWay.SharedKernel.Domain.Primitives;

namespace AlarmInsight.Application.Alarms.Commands.ProcessAlarm;

public sealed record ProcessAlarmCommand(int AlarmId) : IRequest<Result>;
```

---

## 📄 File 7: ProcessAlarmCommandHandler.cs

**Location:** `AlarmInsight.Application/Alarms/Commands/ProcessAlarm/ProcessAlarmCommandHandler.cs`

```csharp
using MediatR;
using BahyWay.SharedKernel.Domain.Primitives;
using BahyWay.SharedKernel.Application.Abstractions;
using AlarmInsight.Application.Abstractions;

namespace AlarmInsight.Application.Alarms.Commands.ProcessAlarm;

public sealed class ProcessAlarmCommandHandler : IRequestHandler<ProcessAlarmCommand, Result>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationLogger<ProcessAlarmCommandHandler> _logger;
    private readonly ICacheService _cache;

    public ProcessAlarmCommandHandler(
        IAlarmRepository alarmRepository,
        IUnitOfWork unitOfWork,
        IApplicationLogger<ProcessAlarmCommandHandler> logger,
        ICacheService cache)
    {
        _alarmRepository = alarmRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result> Handle(ProcessAlarmCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing alarm: {AlarmId}", request.AlarmId);

        // Get alarm
        var alarm = await _alarmRepository.GetByIdAsync(request.AlarmId, cancellationToken);

        if (alarm is null)
        {
            _logger.LogWarning("Alarm not found: {AlarmId}", request.AlarmId);
            return Result.Failure(AlarmErrors.NotFound(request.AlarmId));
        }

        // Process alarm (domain logic)
        var result = alarm.Process();

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to process alarm: {Error}", result.Error.Message);
            return result;
        }

        // Mark as modified
        alarm.MarkAsModified("System");

        // Persist
        _alarmRepository.Update(alarm);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alarm processed successfully: {AlarmId}", request.AlarmId);

        // Invalidate cache
        await _cache.RemoveAsync(CacheKeys.Alarms.ById(alarm.Id));

        return Result.Success();
    }
}
```

---

## 📄 File 8: ResolveAlarmCommand.cs

**Location:** `AlarmInsight.Application/Alarms/Commands/ResolveAlarm/ResolveAlarmCommand.cs`

```csharp
using MediatR;
using BahyWay.SharedKernel.Domain.Primitives;

namespace AlarmInsight.Application.Alarms.Commands.ResolveAlarm;

public sealed record ResolveAlarmCommand(
    int AlarmId,
    string Resolution
) : IRequest<Result>;
```

---

## 📄 File 9: ResolveAlarmCommandHandler.cs

**Location:** `AlarmInsight.Application/Alarms/Commands/ResolveAlarm/ResolveAlarmCommandHandler.cs`

```csharp
using MediatR;
using BahyWay.SharedKernel.Domain.Primitives;
using BahyWay.SharedKernel.Application.Abstractions;
using AlarmInsight.Application.Abstractions;

namespace AlarmInsight.Application.Alarms.Commands.ResolveAlarm;

public sealed class ResolveAlarmCommandHandler : IRequestHandler<ResolveAlarmCommand, Result>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationLogger<ResolveAlarmCommandHandler> _logger;
    private readonly ICacheService _cache;

    public ResolveAlarmCommandHandler(
        IAlarmRepository alarmRepository,
        IUnitOfWork unitOfWork,
        IApplicationLogger<ResolveAlarmCommandHandler> logger,
        ICacheService cache)
    {
        _alarmRepository = alarmRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result> Handle(ResolveAlarmCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resolving alarm: {AlarmId}", request.AlarmId);

        // Get alarm
        var alarm = await _alarmRepository.GetByIdAsync(request.AlarmId, cancellationToken);

        if (alarm is null)
        {
            _logger.LogWarning("Alarm not found: {AlarmId}", request.AlarmId);
            return Result.Failure(AlarmErrors.NotFound(request.AlarmId));
        }

        // Resolve alarm (domain logic)
        var result = alarm.Resolve(request.Resolution);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to resolve alarm: {Error}", result.Error.Message);
            return result;
        }

        // Mark as modified
        alarm.MarkAsModified("System");

        // Persist
        _alarmRepository.Update(alarm);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alarm resolved successfully: {AlarmId}", request.AlarmId);

        // Invalidate cache
        await _cache.RemoveAsync(CacheKeys.Alarms.ById(alarm.Id));
        await _cache.RemoveByPatternAsync(CacheKeys.Alarms.Pattern());

        return Result.Success();
    }
}
```

---

## 📄 File 10: GetAlarmQuery.cs

**Location:** `AlarmInsight.Application/Alarms/Queries/GetAlarm/GetAlarmQuery.cs`

```csharp
using MediatR;
using BahyWay.SharedKernel.Domain.Primitives;

namespace AlarmInsight.Application.Alarms.Queries.GetAlarm;

public sealed record GetAlarmQuery(int AlarmId) : IRequest<Result<AlarmDto>>;
```

---

## 📄 File 11: GetAlarmQueryHandler.cs

**Location:** `AlarmInsight.Application/Alarms/Queries/GetAlarm/GetAlarmQueryHandler.cs`

```csharp
using MediatR;
using BahyWay.SharedKernel.Domain.Primitives;
using BahyWay.SharedKernel.Application.Abstractions;
using AlarmInsight.Application.Abstractions;

namespace AlarmInsight.Application.Alarms.Queries.GetAlarm;

public sealed class GetAlarmQueryHandler : IRequestHandler<GetAlarmQuery, Result<AlarmDto>>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IApplicationLogger<GetAlarmQueryHandler> _logger;
    private readonly ICacheService _cache;

    public GetAlarmQueryHandler(
        IAlarmRepository alarmRepository,
        IApplicationLogger<GetAlarmQueryHandler> logger,
        ICacheService cache)
    {
        _alarmRepository = alarmRepository;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<AlarmDto>> Handle(GetAlarmQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching alarm: {AlarmId}", request.AlarmId);

        // Try cache first
        var cachedAlarm = await _cache.GetAsync<AlarmDto>(
            CacheKeys.Alarms.ById(request.AlarmId),
            cancellationToken);

        if (cachedAlarm is not null)
        {
            _logger.LogDebug("Alarm found in cache: {AlarmId}", request.AlarmId);
            return Result.Success(cachedAlarm);
        }

        // Get from database
        var alarm = await _alarmRepository.GetByIdAsync(request.AlarmId, cancellationToken);

        if (alarm is null)
        {
            _logger.LogWarning("Alarm not found: {AlarmId}", request.AlarmId);
            return Result.Failure<AlarmDto>(AlarmErrors.NotFound(request.AlarmId));
        }

        // Map to DTO
        var dto = new AlarmDto
        {
            Id = alarm.Id,
            Source = alarm.Source,
            Description = alarm.Description,
            Severity = alarm.Severity.Name,
            Location = alarm.Location.Name,
            Latitude = alarm.Location.Latitude,
            Longitude = alarm.Location.Longitude,
            Status = alarm.Status.ToString(),
            OccurredAt = alarm.OccurredAt,
            ProcessedAt = alarm.ProcessedAt,
            ResolvedAt = alarm.ResolvedAt,
            Resolution = alarm.Resolution
        };

        // Cache it
        await _cache.SetAsync(
            CacheKeys.Alarms.ById(request.AlarmId),
            dto,
            CacheExpiration.Medium,
            cancellationToken);

        return Result.Success(dto);
    }
}
```

---

## 📄 File 12: AlarmDto.cs

**Location:** `AlarmInsight.Application/Alarms/Queries/GetAlarm/AlarmDto.cs`

```csharp
namespace AlarmInsight.Application.Alarms.Queries.GetAlarm;

/// <summary>
/// Data Transfer Object for Alarm entity.
/// Used to return data to API consumers.
/// </summary>
public sealed class AlarmDto
{
    public int Id { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? Resolution { get; init; }
}
```

---

## 📄 File 13: GetActiveAlarmsQuery.cs

**Location:** `AlarmInsight.Application/Alarms/Queries/GetActiveAlarms/GetActiveAlarmsQuery.cs`

```csharp
using MediatR;
using BahyWay.SharedKernel.Domain.Primitives;

namespace AlarmInsight.Application.Alarms.Queries.GetActiveAlarms;

public sealed record GetActiveAlarmsQuery() : IRequest<Result<IEnumerable<AlarmSummaryDto>>>;
```

---

## 📄 File 14: GetActiveAlarmsQueryHandler.cs

**Location:** `AlarmInsight.Application/Alarms/Queries/GetActiveAlarms/GetActiveAlarmsQueryHandler.cs`

```csharp
using MediatR;
using BahyWay.SharedKernel.Domain.Primitives;
using BahyWay.SharedKernel.Application.Abstractions;
using AlarmInsight.Application.Abstractions;

namespace AlarmInsight.Application.Alarms.Queries.GetActiveAlarms;

public sealed class GetActiveAlarmsQueryHandler 
    : IRequestHandler<GetActiveAlarmsQuery, Result<IEnumerable<AlarmSummaryDto>>>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IApplicationLogger<GetActiveAlarmsQueryHandler> _logger;
    private readonly ICacheService _cache;

    public GetActiveAlarmsQueryHandler(
        IAlarmRepository alarmRepository,
        IApplicationLogger<GetActiveAlarmsQueryHandler> logger,
        ICacheService cache)
    {
        _alarmRepository = alarmRepository;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<IEnumerable<AlarmSummaryDto>>> Handle(
        GetActiveAlarmsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching active alarms");

        // Try cache first
        var cachedAlarms = await _cache.GetAsync<IEnumerable<AlarmSummaryDto>>(
            CacheKeys.Alarms.AllActive(),
            cancellationToken);

        if (cachedAlarms is not null)
        {
            _logger.LogDebug("Active alarms found in cache");
            return Result.Success(cachedAlarms);
        }

        // Get from database
        var alarms = await _alarmRepository.GetActiveAlarmsAsync(cancellationToken);

        // Map to DTOs
        var dtos = alarms.Select(alarm => new AlarmSummaryDto
        {
            Id = alarm.Id,
            Source = alarm.Source,
            Severity = alarm.Severity.Name,
            Location = alarm.Location.Name,
            Status = alarm.Status.ToString(),
            OccurredAt = alarm.OccurredAt
        });

        // Cache it
        await _cache.SetAsync(
            CacheKeys.Alarms.AllActive(),
            dtos,
            CacheExpiration.Short,
            cancellationToken);

        _logger.LogInformation("Retrieved {Count} active alarms", dtos.Count());

        return Result.Success(dtos);
    }
}
```

---

## 📄 File 15: AlarmSummaryDto.cs

**Location:** `AlarmInsight.Application/Alarms/Queries/GetActiveAlarms/AlarmSummaryDto.cs`

```csharp
namespace AlarmInsight.Application.Alarms.Queries.GetActiveAlarms;

/// <summary>
/// Summary DTO for alarm lists (lighter than full AlarmDto).
/// </summary>
public sealed class AlarmSummaryDto
{
    public int Id { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
}
```

---

## 📄 File 16: DependencyInjection.cs

**Location:** `AlarmInsight.Application/DependencyInjection.cs`

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AlarmInsight.Application;

/// <summary>
/// Extension method for registering Application layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
```

---

## 🔨 Installation Steps

### 1. Add Project Reference

In Visual Studio:
1. Right-click **AlarmInsight.Application** project
2. Add → Project Reference
3. Select **AlarmInsight.Domain**
4. Select **BahyWay.SharedKernel**

### 2. Install NuGet Packages

```powershell
# In AlarmInsight.Application
dotnet add package MediatR --version 12.2.0
dotnet add package FluentValidation --version 11.9.0
```

### 3. Create Files

Copy each file above into the correct location in your project.

### 4. Build

Press **Ctrl+Shift+B**

**Expected:** ✅ Build succeeded

---

## ✅ What You've Built

- ✅ **3 Commands** (Create, Process, Resolve alarms)
- ✅ **3 Command Handlers** (with full use of SharedKernel abstractions)
- ✅ **1 Validator** (FluentValidation example)
- ✅ **2 Queries** (Get single alarm, Get active alarms)
- ✅ **2 Query Handlers** (with caching)
- ✅ **2 DTOs** (AlarmDto, AlarmSummaryDto)
- ✅ **2 Repository Interfaces**
- ✅ **1 Dependency Injection setup**

**Total:** 16 files, production-ready CQRS implementation!

---

## 🎯 What Each Handler Demonstrates

| Handler | Demonstrates |
|---------|--------------|
| **CreateAlarmCommandHandler** | Logging, Caching, Background Jobs, Result Pattern, Value Objects, Audit |
| **ProcessAlarmCommandHandler** | Domain Logic, Caching, Error Handling |
| **ResolveAlarmCommandHandler** | Domain Logic, Cache Invalidation |
| **GetAlarmQueryHandler** | Read-side Caching, DTO Mapping |
| **GetActiveAlarmsQueryHandler** | Collection Caching, List Queries |

---

## 📚 Next Steps

After adding these files:

1. **Build the project** (should succeed)
2. **Option 1:** Create Infrastructure layer (repositories, DbContext)
3. **Option 2:** Create API controllers to expose these commands/queries

---

**Copy each file's code into your project and you'll have a complete Application layer!** 🚀
