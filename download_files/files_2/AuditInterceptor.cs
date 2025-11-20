using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BahyWay.SharedKernel.Infrastructure.Audit;

/// <summary>
/// EF Core interceptor that automatically populates audit fields when saving changes.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationLogger<AuditInterceptor> _logger;

    public AuditInterceptor(
        ICurrentUserService currentUserService,
        IApplicationLogger<AuditInterceptor> logger)
    {
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext context)
    {
        if (context == null)
            return;

        var currentUser = _currentUserService.UserId ?? "System";
        var currentTime = DateTime.UtcNow;

        var entries = context.ChangeTracker.Entries<AuditableEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.MarkAsCreated(currentUser, currentTime);
                    _logger.LogDebug(
                        "Entity {EntityType} with ID {EntityId} created by {User}",
                        entry.Entity.GetType().Name,
                        entry.Entity.Id,
                        currentUser);
                    break;

                case EntityState.Modified:
                    entry.Entity.MarkAsModified(currentUser, currentTime);
                    _logger.LogDebug(
                        "Entity {EntityType} with ID {EntityId} modified by {User}",
                        entry.Entity.GetType().Name,
                        entry.Entity.Id,
                        currentUser);
                    break;
            }
        }

        // Handle soft deletes
        var softDeletableEntries = context.ChangeTracker.Entries<SoftDeletableAuditableEntity>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (var entry in softDeletableEntries)
        {
            entry.State = EntityState.Modified;
            entry.Entity.MarkAsDeleted(currentUser, currentTime);
            
            _logger.LogInformation(
                "Entity {EntityType} with ID {EntityId} soft deleted by {User}",
                entry.Entity.GetType().Name,
                entry.Entity.Id,
                currentUser);
        }
    }
}

/// <summary>
/// Service to get the current authenticated user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID or identifier.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Gets the current user's email.
    /// </summary>
    string Email { get; }

    /// <summary>
    /// Gets whether a user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}

/// <summary>
/// Implementation of current user service using ASP.NET Core HttpContext.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId => 
        _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
        ?? "Anonymous";

    public string Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;

    public bool IsAuthenticated => 
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

/// <summary>
/// Audit log entry for detailed change tracking.
/// Store in separate table for compliance and investigation.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }
    public string Action { get; set; } // Created, Modified, Deleted
    public string UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Changes { get; set; } // JSON of changed properties
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
}

/// <summary>
/// Service for querying audit logs.
/// </summary>
public interface IAuditQueryService
{
    Task<List<AuditLog>> GetEntityHistoryAsync(string entityType, string entityId);
    Task<List<AuditLog>> GetUserActivityAsync(string userId, DateTime from, DateTime to);
    Task<List<AuditLog>> GetRecentChangesAsync(int count = 100);
}
