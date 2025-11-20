# BahyWay Architecture - Missing Components Implementation Roadmap

## Overview
This document outlines the implementation plan for all critical missing architectural components across the BahyWay ecosystem.

---

## 🎯 Missing Components Summary

### 1. **FileWatcher / WatchDog Service** ⭐ (You identified this!)
**Purpose:** Monitor folders for incoming files (especially large zip files for ETLway)

**Key Features:**
- Monitor multiple directories simultaneously
- Handle large file uploads (wait until fully written)
- File locking detection (don't process files still being written)
- Automatic unzipping and processing
- Retry on failures
- Dead letter queue for failed files
- Progress tracking for large files

**Critical For:**
- ETLway - Data file imports
- HireWay - Resume batch uploads
- SSISight - SSIS package imports
- AlarmInsight - Historical alarm data imports

---

### 2. **Observability Stack**
- Structured Logging (Serilog)
- Distributed Tracing (OpenTelemetry)
- Metrics Collection (Prometheus-compatible)
- Health Checks
- Correlation IDs across services

---

### 3. **Background Jobs & Scheduling**
- Hangfire for job orchestration
- Recurring jobs
- Job monitoring dashboard
- Retry policies
- Job persistence

---

### 4. **Caching Infrastructure**
- In-memory caching (IMemoryCache)
- Distributed caching (Redis)
- Cache invalidation strategies
- Cache-aside pattern

---

### 5. **Event Bus / Message Queue**
- MassTransit + RabbitMQ
- Event publishing/subscribing
- Saga orchestration
- Guaranteed delivery

---

### 6. **Audit Logging & Change Tracking**
- Auditable entities
- EF Core interceptors
- Temporal tables
- Audit queries

---

### 7. **Data Migration Strategy**
- FluentMigrator or EF Core Migrations
- Seeding strategy
- Migration testing
- Rollback procedures

---

### 8. **Resiliency Patterns**
- Polly policies (Retry, Circuit Breaker, Timeout)
- Fallback strategies
- Bulkhead isolation
- Rate limiting

---

### 9. **File Storage Abstraction**
- IFileStorageService interface
- Local file system provider
- Cloud storage providers (Azure Blob, AWS S3)
- Configuration-driven selection

---

### 10. **Notification System**
- Email service (SMTP/SendGrid)
- SMS service (Twilio)
- Push notifications
- Template engine (Scriban)

---

### 11. **API Documentation**
- Swagger/OpenAPI
- API versioning
- XML documentation
- Contract testing

---

## 📅 Implementation Timeline (16 Weeks)

### **Phase 1: Foundation (Weeks 1-4)**

#### Week 1: Observability Foundation
- [ ] Serilog structured logging setup
- [ ] Correlation ID middleware
- [ ] Health check infrastructure
- [ ] Basic metrics collection
- [ ] Log enrichment

#### Week 2: FileWatcher/WatchDog Service
- [ ] File monitoring service base
- [ ] Large file handling (chunked reading)
- [ ] File lock detection
- [ ] Zip extraction utilities
- [ ] Integration with background jobs

#### Week 3: Background Jobs Infrastructure
- [ ] Hangfire setup with PostgreSQL
- [ ] Job abstraction layer
- [ ] Recurring job scheduler
- [ ] Job monitoring dashboard
- [ ] Retry policies

#### Week 4: Caching Infrastructure
- [ ] IMemoryCache implementation
- [ ] Redis setup and configuration
- [ ] Cache service abstraction
- [ ] Cache invalidation patterns
- [ ] Performance testing

---

### **Phase 2: Integration & Resilience (Weeks 5-8)**

#### Week 5: Event Bus Implementation
- [ ] MassTransit + RabbitMQ setup
- [ ] Event base classes
- [ ] Publishing infrastructure
- [ ] Consumer base classes
- [ ] Dead letter queue handling

#### Week 6: Audit Logging
- [ ] AuditableEntity base class
- [ ] EF Core audit interceptor
- [ ] Audit table design
- [ ] Audit query services
- [ ] Temporal table setup

#### Week 7: Resiliency Patterns
- [ ] Polly policy configurations
- [ ] HTTP client resilience
- [ ] Database resilience
- [ ] Circuit breaker implementation
- [ ] Fallback strategies

#### Week 8: File Storage Abstraction
- [ ] IFileStorageService interface
- [ ] Local file system provider
- [ ] Azure Blob Storage provider
- [ ] AWS S3 provider (optional)
- [ ] Configuration system

---

### **Phase 3: Advanced Features (Weeks 9-12)**

#### Week 9: Notification System
- [ ] Email service (SendGrid)
- [ ] SMS service (Twilio)
- [ ] Template engine (Scriban)
- [ ] Notification queue
- [ ] Retry logic

#### Week 10: Data Migration Strategy
- [ ] Migration framework setup
- [ ] Migration base classes
- [ ] Seeding strategy
- [ ] Migration testing
- [ ] CI/CD integration

#### Week 11: API Documentation & Versioning
- [ ] Swagger/OpenAPI setup
- [ ] XML documentation
- [ ] API versioning strategy
- [ ] Contract tests
- [ ] Client SDK generation

#### Week 12: Advanced Observability
- [ ] Distributed tracing (OpenTelemetry)
- [ ] Application Performance Monitoring
- [ ] Business metrics dashboards
- [ ] Alert configuration
- [ ] Log aggregation

---

### **Phase 4: Project Integration (Weeks 13-16)**

#### Week 13: ETLway Integration
- [ ] FileWatcher for data imports
- [ ] Background job processing
- [ ] Caching for metadata
- [ ] Event publishing
- [ ] Observability integration

#### Week 14: AlarmInsight Integration
- [ ] Real-time alarm processing
- [ ] Rules engine integration
- [ ] Event publishing to SteerView
- [ ] Caching strategy
- [ ] Notification system

#### Week 15: SteerView & NajafCemetery
- [ ] Geospatial caching
- [ ] File storage for images
- [ ] Background job for report generation
- [ ] Notification system
- [ ] Audit logging

#### Week 16: HireWay & SmartForesight
- [ ] Resume processing (FileWatcher)
- [ ] Email notifications
- [ ] Model training (background jobs)
- [ ] Forecast caching
- [ ] Comprehensive testing

---

## 🏗️ SharedKernel Structure (Updated)

```
SharedKernel/
├── Domain/
│   ├── Primitives/
│   │   ├── Entity.cs ✓
│   │   ├── ValueObject.cs ✓
│   │   ├── AggregateRoot.cs ✓
│   │   ├── AuditableEntity.cs ⭐ NEW
│   │   └── DomainEvent.cs ⭐ NEW
│   ├── Results/
│   │   ├── Result.cs ✓
│   │   └── ResultExtensions.cs ✓
│   └── Guards/
│       └── Guard.cs ✓
│
├── Application/
│   ├── Abstractions/
│   │   ├── IRepository.cs ✓
│   │   ├── IUnitOfWork.cs ✓
│   │   ├── ICacheService.cs ⭐ NEW
│   │   ├── IEventBus.cs ⭐ NEW
│   │   ├── IBackgroundJobService.cs ⭐ NEW
│   │   ├── IFileStorageService.cs ⭐ NEW
│   │   ├── IFileWatcherService.cs ⭐ NEW
│   │   ├── INotificationService.cs ⭐ NEW
│   │   └── IAuditService.cs ⭐ NEW
│   ├── Behaviors/
│   │   ├── LoggingBehavior.cs ⭐ NEW
│   │   ├── ValidationBehavior.cs ✓
│   │   ├── CachingBehavior.cs ⭐ NEW
│   │   └── PerformanceBehavior.cs ⭐ NEW
│   └── Events/
│       ├── IntegrationEvent.cs ⭐ NEW
│       └── EventBusExtensions.cs ⭐ NEW
│
├── Infrastructure/
│   ├── Observability/
│   │   ├── Logging/
│   │   │   ├── LoggingConfiguration.cs ⭐ NEW
│   │   │   ├── CorrelationIdMiddleware.cs ⭐ NEW
│   │   │   └── LogEnrichers.cs ⭐ NEW
│   │   ├── Metrics/
│   │   │   ├── MetricsCollector.cs ⭐ NEW
│   │   │   └── CustomMetrics.cs ⭐ NEW
│   │   ├── Tracing/
│   │   │   ├── TracingConfiguration.cs ⭐ NEW
│   │   │   └── ActivityEnrichers.cs ⭐ NEW
│   │   └── HealthChecks/
│   │       ├── DatabaseHealthCheck.cs ⭐ NEW
│   │       ├── RedisHealthCheck.cs ⭐ NEW
│   │       └── RabbitMqHealthCheck.cs ⭐ NEW
│   │
│   ├── Caching/
│   │   ├── CacheService.cs ⭐ NEW
│   │   ├── RedisCacheService.cs ⭐ NEW
│   │   ├── CacheKeyBuilder.cs ⭐ NEW
│   │   └── CacheConfiguration.cs ⭐ NEW
│   │
│   ├── BackgroundJobs/
│   │   ├── HangfireJobService.cs ⭐ NEW
│   │   ├── JobConfiguration.cs ⭐ NEW
│   │   └── RecurringJobsSetup.cs ⭐ NEW
│   │
│   ├── FileWatcher/
│   │   ├── FileWatcherService.cs ⭐ NEW
│   │   ├── FileWatcherConfiguration.cs ⭐ NEW
│   │   ├── LargeFileHandler.cs ⭐ NEW
│   │   ├── ZipFileProcessor.cs ⭐ NEW
│   │   └── FileLockDetector.cs ⭐ NEW
│   │
│   ├── EventBus/
│   │   ├── MassTransitEventBus.cs ⭐ NEW
│   │   ├── EventBusConfiguration.cs ⭐ NEW
│   │   └── ConsumerBase.cs ⭐ NEW
│   │
│   ├── Resilience/
│   │   ├── ResiliencePolicies.cs ⭐ NEW
│   │   ├── CircuitBreakerConfiguration.cs ⭐ NEW
│   │   └── RetryPolicies.cs ⭐ NEW
│   │
│   ├── FileStorage/
│   │   ├── LocalFileStorageService.cs ⭐ NEW
│   │   ├── AzureBlobStorageService.cs ⭐ NEW
│   │   ├── AwsS3StorageService.cs ⭐ NEW
│   │   └── FileStorageConfiguration.cs ⭐ NEW
│   │
│   ├── Notifications/
│   │   ├── EmailService.cs ⭐ NEW
│   │   ├── SmsService.cs ⭐ NEW
│   │   ├── TemplateEngine.cs ⭐ NEW
│   │   └── NotificationQueue.cs ⭐ NEW
│   │
│   ├── Persistence/
│   │   ├── Interceptors/
│   │   │   ├── AuditInterceptor.cs ⭐ NEW
│   │   │   ├── SoftDeleteInterceptor.cs ⭐ NEW
│   │   │   └── DomainEventInterceptor.cs ⭐ NEW
│   │   ├── Migrations/
│   │   │   └── BaseMigration.cs ⭐ NEW
│   │   └── Configurations/
│   │       └── AuditConfiguration.cs ⭐ NEW
│   │
│   └── Api/
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs ⭐ NEW
│       │   ├── CorrelationIdMiddleware.cs ⭐ NEW
│       │   └── RequestLoggingMiddleware.cs ⭐ NEW
│       ├── Filters/
│       │   ├── ValidationFilter.cs ⭐ NEW
│       │   └── PerformanceFilter.cs ⭐ NEW
│       └── Swagger/
│           ├── SwaggerConfiguration.cs ⭐ NEW
│           └── SwaggerFilters.cs ⭐ NEW
│
└── CrossCutting/
    ├── Extensions/
    │   ├── ServiceCollectionExtensions.cs ⭐ NEW
    │   ├── ApplicationBuilderExtensions.cs ⭐ NEW
    │   └── ConfigurationExtensions.cs ⭐ NEW
    ├── Constants/
    │   ├── CacheKeys.cs ⭐ NEW
    │   ├── LoggingConstants.cs ⭐ NEW
    │   └── MetricNames.cs ⭐ NEW
    └── Utilities/
        ├── FileUtilities.cs ⭐ NEW
        ├── CompressionUtilities.cs ⭐ NEW
        └── HashingUtilities.cs ⭐ NEW
```

---

## 📦 Required NuGet Packages

### Observability
```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.Seq" Version="6.0.0" />
<PackageReference Include="Serilog.Enrichers.Environment" Version="2.3.0" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="3.1.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.7.0-rc.1" />
```

### Background Jobs
```xml
<PackageReference Include="Hangfire.Core" Version="1.8.9" />
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.9" />
<PackageReference Include="Hangfire.PostgreSql" Version="1.20.6" />
```

### Caching
```xml
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
<PackageReference Include="StackExchange.Redis" Version="2.7.10" />
```

### Event Bus
```xml
<PackageReference Include="MassTransit" Version="8.1.3" />
<PackageReference Include="MassTransit.RabbitMQ" Version="8.1.3" />
<PackageReference Include="MassTransit.AspNetCore" Version="7.3.1" />
```

### Resilience
```xml
<PackageReference Include="Polly" Version="8.2.0" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
```

### File Storage
```xml
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
<PackageReference Include="AWSSDK.S3" Version="3.7.305" />
```

### Notifications
```xml
<PackageReference Include="SendGrid" Version="9.28.1" />
<PackageReference Include="Twilio" Version="6.16.1" />
<PackageReference Include="Scriban" Version="5.9.1" />
```

### File Handling
```xml
<PackageReference Include="SharpZipLib" Version="1.4.2" />
<PackageReference Include="System.IO.Compression" Version="4.3.0" />
```

### API Documentation
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Versioning" Version="5.1.0" />
```

### Health Checks
```xml
<PackageReference Include="AspNetCore.HealthChecks.Npgsql" Version="8.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.Redis" Version="8.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.RabbitMQ" Version="8.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.Hangfire" Version="8.0.0" />
```

---

## 🎯 Success Metrics

### Observability
- ✅ All requests have correlation IDs
- ✅ 100% of errors logged with context
- ✅ Health checks respond < 500ms
- ✅ Metrics collected for all critical operations

### Performance
- ✅ 90% cache hit rate for frequently accessed data
- ✅ Background jobs process < 5 minutes
- ✅ File watcher processes files within 10 seconds
- ✅ API response times < 200ms (p95)

### Reliability
- ✅ 99.9% uptime
- ✅ Zero data loss in event bus
- ✅ All transient errors automatically retried
- ✅ Circuit breakers prevent cascading failures

### Audit & Compliance
- ✅ 100% of data changes audited
- ✅ Audit logs retained for 7 years
- ✅ User actions traceable
- ✅ Compliance reports generated automatically

---

## 🚀 Next Steps

1. **Review and approve this roadmap**
2. **Set up development environment** (Redis, RabbitMQ, Seq/ELK)
3. **Start with Phase 1, Week 1** (Observability Foundation)
4. **Implement components incrementally** following the timeline
5. **Test each component** before moving to the next
6. **Document** as you go

---

## 📚 Additional Resources

- **Serilog Best Practices:** https://github.com/serilog/serilog/wiki/Best-Practices
- **Hangfire Documentation:** https://docs.hangfire.io/
- **MassTransit Patterns:** https://masstransit.io/documentation/patterns
- **Polly Resilience:** https://github.com/App-vNext/Polly
- **OpenTelemetry .NET:** https://opentelemetry.io/docs/instrumentation/net/

---

**Document Version:** 1.0  
**Last Updated:** November 16, 2025  
**Author:** BahyWay Architecture Team
