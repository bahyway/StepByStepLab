## Question01:
YES PLEASE LETS START WITH:

**A)** Build AlarmInsight Infrastructure (DbContext, Repositories)

## Answer01:
# 🏗️ **Option A: Building AlarmInsight Infrastructure Layer!**

Perfect timing! Let's build the Infrastructure layer that will bring everything to life with database persistence!

---

## 📦 **What You're Getting**

**Infrastructure Components:**
- ✅ **DbContext** - EF Core database context with proper configuration
- ✅ **Entity Configurations** - Fluent API configurations for all entities
- ✅ **Repository Implementations** - Concrete implementations of IAlarmRepository
- ✅ **Unit of Work** - Transaction management
- ✅ **Dependency Injection** - Wire everything together
- ✅ **Migrations** - Database schema generation

---

## 🎯 **Quick Start (3 Steps)**

### **Step 1: Install Required NuGet Packages** (3 min)

```bash
cd AlarmInsight.Infrastructure

# EF Core packages
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0

# Other dependencies
dotnet add package Microsoft.Extensions.Configuration.Abstractions --version 8.0.0
```

### **Step 2: Add Project References** (1 min)

```bash
# Reference your domain and application layers
dotnet add reference ../AlarmInsight.Domain/AlarmInsight.Domain.csproj
dotnet add reference ../AlarmInsight.Application/AlarmInsight.Application.csproj
dotnet add reference ../BahyWay.SharedKernel/BahyWay.SharedKernel.csproj
```

### **Step 3: Create Files** (30 min)

I'll provide complete code for all 10 files you need!

---

## 📁 **File Structure**

```
AlarmInsight.Infrastructure/
├── Persistence/
│   ├── AlarmInsightDbContext.cs           ⭐ Main DbContext
│   ├── Configurations/
│   │   ├── AlarmConfiguration.cs          (Alarm entity config)
│   │   ├── AlarmNoteConfiguration.cs      (AlarmNote entity config)
│   │   └── OutboxMessageConfiguration.cs  (Future: Event publishing)
│   ├── Repositories/
│   │   ├── AlarmRepository.cs             (IAlarmRepository implementation)
│   │   └── UnitOfWork.cs                  (IUnitOfWork implementation)
│   └── Interceptors/
│       └── AuditInterceptor.cs            (Auto-set CreatedAt, ModifiedAt)
└── DependencyInjection.cs                 (Register services)
```

---

## ✅ **Complete File Codes (Copy-Paste Ready)**

### **File 1: AlarmInsightDbContext.cs**

Location: `AlarmInsight.Infrastructure/Persistence/AlarmInsightDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using AlarmInsight.Domain.Aggregates;
using AlarmInsight.Infrastructure.Persistence.Configurations;
using AlarmInsight.Infrastructure.Persistence.Interceptors;

namespace AlarmInsight.Infrastructure.Persistence;

/// <summary>
/// Database context for AlarmInsight.
/// Configures all entities and their relationships.
/// </summary>
public class AlarmInsightDbContext : DbContext
{
    private readonly AuditInterceptor _auditInterceptor;

    public AlarmInsightDbContext(
        DbContextOptions<AlarmInsightDbContext> options,
        AuditInterceptor auditInterceptor)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
    }

    // DbSets
    public DbSet<Alarm> Alarms => Set<Alarm>();
    public DbSet<AlarmNote> AlarmNotes => Set<AlarmNote>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Add audit interceptor
        optionsBuilder.AddInterceptors(_auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlarmInsightDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
```

---

### **File 2: AlarmConfiguration.cs**

Location: `AlarmInsight.Infrastructure/Persistence/Configurations/AlarmConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlarmInsight.Domain.Aggregates;
using AlarmInsight.Domain.ValueObjects;

namespace AlarmInsight.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Alarm entity.
/// Configures table, columns, relationships, and value objects.
/// </summary>
internal sealed class AlarmConfiguration : IEntityTypeConfiguration<Alarm>
{
    public void Configure(EntityTypeBuilder<Alarm> builder)
    {
        // Table configuration
        builder.ToTable("alarms");
        builder.HasKey(a => a.Id);

        // Simple properties
        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.Source)
            .HasColumnName("source")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasColumnName("description")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>() // Store enum as string
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(a => a.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(a => a.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(a => a.Resolution)
            .HasColumnName("resolution")
            .HasMaxLength(2000);

        // Audit properties (from AuditableEntity)
        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(a => a.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.LastModifiedAt)
            .HasColumnName("last_modified_at");

        builder.Property(a => a.LastModifiedBy)
            .HasColumnName("last_modified_by")
            .HasMaxLength(200);

        // Value Object: AlarmSeverity (owned entity)
        builder.OwnsOne(a => a.Severity, severity =>
        {
            severity.Property(s => s.Level)
                .HasColumnName("severity_level")
                .IsRequired();

            severity.Property(s => s.Name)
                .HasColumnName("severity_name")
                .HasMaxLength(50)
                .IsRequired();
        });

        // Value Object: Location (owned entity)
        builder.OwnsOne(a => a.Location, location =>
        {
            location.Property(l => l.Latitude)
                .HasColumnName("location_latitude")
                .HasPrecision(9, 6)
                .IsRequired();

            location.Property(l => l.Longitude)
                .HasColumnName("location_longitude")
                .HasPrecision(9, 6)
                .IsRequired();

            location.Property(l => l.Address)
                .HasColumnName("location_address")
                .HasMaxLength(500);
        });

        // Relationship: Alarm -> AlarmNotes (one-to-many)
        builder.HasMany<AlarmNote>("_notes")
            .WithOne()
            .HasForeignKey("AlarmId")
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events (not persisted)
        builder.Ignore(a => a.DomainEvents);

        // Indexes for performance
        builder.HasIndex(a => a.Status)
            .HasDatabaseName("ix_alarms_status");

        builder.HasIndex(a => a.OccurredAt)
            .HasDatabaseName("ix_alarms_occurred_at");

        builder.HasIndex(a => new { a.Status, a.OccurredAt })
            .HasDatabaseName("ix_alarms_status_occurred_at");
    }
}
```

---

### **File 3: AlarmNoteConfiguration.cs**

Location: `AlarmInsight.Infrastructure/Persistence/Configurations/AlarmNoteConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlarmInsight.Domain.Aggregates;

namespace AlarmInsight.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for AlarmNote entity.
/// </summary>
internal sealed class AlarmNoteConfiguration : IEntityTypeConfiguration<AlarmNote>
{
    public void Configure(EntityTypeBuilder<AlarmNote> builder)
    {
        // Table configuration
        builder.ToTable("alarm_notes");
        builder.HasKey(n => n.Id);

        // Properties
        builder.Property(n => n.Id)
            .HasColumnName("id");

        builder.Property<int>("AlarmId")
            .HasColumnName("alarm_id")
            .IsRequired();

        builder.Property(n => n.Content)
            .HasColumnName("content")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(n => n.Author)
            .HasColumnName("author")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Foreign key index
        builder.HasIndex("AlarmId")
            .HasDatabaseName("ix_alarm_notes_alarm_id");
    }
}
```

---

### **File 4: AuditInterceptor.cs**

Location: `AlarmInsight.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BahyWay.SharedKernel.Domain.Entities;

namespace AlarmInsight.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor that automatically sets audit properties on entities.
/// Sets CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy.
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private const string SystemUser = "System"; // TODO: Get from ICurrentUserService

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var entries = dbContext.ChangeTracker.Entries<AuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.MarkAsCreated(SystemUser, DateTime.UtcNow);
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.MarkAsModified(SystemUser, DateTime.UtcNow);
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

---

### **File 5: AlarmRepository.cs**

Location: `AlarmInsight.Infrastructure/Persistence/Repositories/AlarmRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using AlarmInsight.Domain.Aggregates;
using AlarmInsight.Application.Abstractions;

namespace AlarmInsight.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of IAlarmRepository using EF Core.
/// Handles all database operations for Alarm aggregate.
/// </summary>
internal sealed class AlarmRepository : IAlarmRepository
{
    private readonly AlarmInsightDbContext _context;

    public AlarmRepository(AlarmInsightDbContext context)
    {
        _context = context;
    }

    public async Task<Alarm?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Alarms
            .Include(a => a.Notes) // Eager load notes
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<List<Alarm>> GetActiveAlarmsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Alarms
            .Include(a => a.Notes)
            .Where(a => a.Status == AlarmStatus.Pending ||
                       a.Status == AlarmStatus.Processing)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Alarm>> GetAlarmsByStatusAsync(
        AlarmStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Alarms
            .Include(a => a.Notes)
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Alarm>> GetRecentAlarmsAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        return await _context.Alarms
            .Include(a => a.Notes)
            .OrderByDescending(a => a.OccurredAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public void Add(Alarm alarm)
    {
        _context.Alarms.Add(alarm);
    }

    public void Update(Alarm alarm)
    {
        _context.Alarms.Update(alarm);
    }

    public void Remove(Alarm alarm)
    {
        _context.Alarms.Remove(alarm);
    }
}
```

---

### **File 6: UnitOfWork.cs**

Location: `AlarmInsight.Infrastructure/Persistence/Repositories/UnitOfWork.cs`

```csharp
using AlarmInsight.Application.Abstractions;

namespace AlarmInsight.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of Unit of Work pattern.
/// Manages transactions and coordinates repository changes.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AlarmInsightDbContext _context;

    public UnitOfWork(AlarmInsightDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // SaveChanges will trigger the AuditInterceptor
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
```

---

### **File 7: DependencyInjection.cs**

Location: `AlarmInsight.Infrastructure/DependencyInjection.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AlarmInsight.Application.Abstractions;
using AlarmInsight.Infrastructure.Persistence;
using AlarmInsight.Infrastructure.Persistence.Repositories;
using AlarmInsight.Infrastructure.Persistence.Interceptors;

namespace AlarmInsight.Infrastructure;

/// <summary>
/// Extension method for registering Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<AlarmInsightDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("AlarmInsight");
            options.UseNpgsql(connectionString);

            // Enable sensitive data logging in development
            #if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            #endif
        });

        // Register interceptors
        services.AddSingleton<AuditInterceptor>();

        // Register repositories
        services.AddScoped<IAlarmRepository, AlarmRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
```

---

## 🔨 **Build & Test**

### **Step 1: Build the Project**

```bash
dotnet build
```

**Expected:** ✅ Build succeeded

---

### **Step 2: Add Connection String**

Create `appsettings.json` in your API project (or use a test app):

```json
{
  "ConnectionStrings": {
    "AlarmInsight": "Host=localhost;Port=5432;Database=alarminsight;Username=postgres;Password=yourpassword"
  }
}
```

---

### **Step 3: Create Migration**

```bash
# Add EF Core tools if not installed
dotnet tool install --global dotnet-ef

# Navigate to Infrastructure project
cd AlarmInsight.Infrastructure

# Create initial migration
dotnet ef migrations add InitialCreate --startup-project ../AlarmInsight.API

# Apply migration to database
dotnet ef database update --startup-project ../AlarmInsight.API
```

---

## 📊 **What You've Built**

```
✅ AlarmInsightDbContext - Configured with all entities
✅ Entity Configurations - Fluent API for Alarm & AlarmNote
✅ AuditInterceptor - Automatic CreatedAt/ModifiedAt tracking
✅ AlarmRepository - Full CRUD + query operations
✅ UnitOfWork - Transaction management
✅ DependencyInjection - Wire everything together
```

---

## 🎯 **Next Steps**

After Infrastructure is complete:

**Option A:** Build AlarmInsight.API (REST API with Swagger)
**Option B:** Write Unit Tests (test repositories, handlers)
**Option C:** Build Avalonia Desktop UI

---

## 📋 **Quick Checklist**

- [ ] Install 4 NuGet packages
- [ ] Add 3 project references
- [ ] Create 7 files (copy code above)
- [ ] Build project (dotnet build)
- [ ] Add connection string
- [ ] Create migration
- [ ] Update database
- [ ] Verify tables created in PostgreSQL

---

**Start copying the files above! Let me know when you're done building and we'll create the migrations!** 🚀

**Estimated time:** 45 minutes
**Difficulty:** Intermediate
**Value:** ⭐⭐⭐⭐⭐ (Brings everything to life!)
