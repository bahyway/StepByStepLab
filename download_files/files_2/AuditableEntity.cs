namespace BahyWay.SharedKernel.Domain.Entities;

/// <summary>
/// Base class for entities that require audit tracking.
/// Automatically tracks who created/modified the entity and when.
/// </summary>
public abstract class AuditableEntity : Entity
{
    /// <summary>
    /// When the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Who created the entity (user ID, email, or system name).
    /// </summary>
    public string CreatedBy { get; private set; }

    /// <summary>
    /// When the entity was last modified.
    /// </summary>
    public DateTime? LastModifiedAt { get; private set; }

    /// <summary>
    /// Who last modified the entity.
    /// </summary>
    public string LastModifiedBy { get; private set; }

    /// <summary>
    /// Marks the entity as created by a specific user.
    /// </summary>
    public void MarkAsCreated(string createdBy, DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("CreatedBy cannot be null or empty", nameof(createdBy));

        CreatedBy = createdBy;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the entity as modified by a specific user.
    /// </summary>
    public void MarkAsModified(string modifiedBy, DateTime? modifiedAt = null)
    {
        if (string.IsNullOrWhiteSpace(modifiedBy))
            throw new ArgumentException("ModifiedBy cannot be null or empty", nameof(modifiedBy));

        LastModifiedBy = modifiedBy;
        LastModifiedAt = modifiedAt ?? DateTime.UtcNow;
    }
}

/// <summary>
/// Base class for entities that support soft delete with audit tracking.
/// </summary>
public abstract class SoftDeletableAuditableEntity : AuditableEntity
{
    /// <summary>
    /// Indicates whether the entity has been soft deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// When the entity was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Who deleted the entity.
    /// </summary>
    public string DeletedBy { get; private set; }

    /// <summary>
    /// Soft deletes the entity.
    /// </summary>
    public void MarkAsDeleted(string deletedBy, DateTime? deletedAt = null)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("DeletedBy cannot be null or empty", nameof(deletedBy));

        IsDeleted = true;
        DeletedBy = deletedBy;
        DeletedAt = deletedAt ?? DateTime.UtcNow;
        
        MarkAsModified(deletedBy, deletedAt);
    }

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    public void Restore(string restoredBy)
    {
        if (string.IsNullOrWhiteSpace(restoredBy))
            throw new ArgumentException("RestoredBy cannot be null or empty", nameof(restoredBy));

        IsDeleted = false;
        DeletedBy = null;
        DeletedAt = null;
        
        MarkAsModified(restoredBy);
    }
}

// Placeholder for Entity base class (assuming it exists in your SharedKernel)
public abstract class Entity
{
    public int Id { get; protected set; }
    
    // Add your existing Entity implementation here
    // (equality, domain events, etc.)
}
