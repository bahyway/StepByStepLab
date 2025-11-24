# BahyWay SharedKernel - Required NuGet Packages

This document lists all NuGet packages required for the SharedKernel infrastructure components.

## Core Packages (Required for All Projects)

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
```

## 1. Observability & Logging

```xml
<!-- Serilog Core -->
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="Serilog.Extensions.Logging" Version="8.0.0" />

<!-- Serilog Sinks -->
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.Seq" Version="7.0.0" />
<PackageReference Include="Serilog.Sinks.Elasticsearch" Version="10.0.0" />

<!-- Serilog Enrichers -->
<PackageReference Include="Serilog.Enrichers.Environment" Version="2.3.0" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="3.1.0" />
<PackageReference Include="Serilog.Exceptions" Version="8.4.0" />

<!-- Formatting -->
<PackageReference Include="Serilog.Formatting.Compact" Version="2.0.0" />
```

## 2. Distributed Tracing (OpenTelemetry)

```xml
<PackageReference Include="OpenTelemetry" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.SqlClient" Version="1.7.0-beta.1" />
<PackageReference Include="OpenTelemetry.Exporter.Jaeger" Version="1.5.1" />
<PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.7.0" />
```

## 3. Metrics & Monitoring

```xml
<PackageReference Include="System.Diagnostics.DiagnosticSource" Version="8.0.0" />
<PackageReference Include="prometheus-net" Version="8.2.0" />
<PackageReference Include="prometheus-net.AspNetCore" Version="8.2.0" />
```

## 4. Health Checks

```xml
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="8.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.UI" Version="7.0.2" />
<PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="7.1.0" />
<PackageReference Include="AspNetCore.HealthChecks.UI.InMemory.Storage" Version="7.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.Npgsql" Version="7.1.0" />
<PackageReference Include="AspNetCore.HealthChecks.Redis" Version="7.0.1" />
```

## 5. Background Jobs (Hangfire)

```xml
<PackageReference Include="Hangfire.Core" Version="1.8.9" />
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.9" />
<PackageReference Include="Hangfire.PostgreSql" Version="1.20.6" />
```

## 6. Caching

```xml
<!-- In-Memory Caching -->
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />

<!-- Redis Distributed Caching -->
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
<PackageReference Include="StackExchange.Redis" Version="2.7.10" />
```

## 7. Event Bus (MassTransit)

```xml
<PackageReference Include="MassTransit" Version="8.1.3" />
<PackageReference Include="MassTransit.RabbitMQ" Version="8.1.3" />
<PackageReference Include="MassTransit.AspNetCore" Version="8.1.3" />
```

## 8. Resiliency (Polly)

```xml
<PackageReference Include="Polly" Version="8.2.0" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
```

## 9. Entity Framework Core & Audit

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite" Version="8.0.0" />
```

## 10. Data Migration

```xml
<PackageReference Include="FluentMigrator" Version="5.0.0" />
<PackageReference Include="FluentMigrator.Runner" Version="5.0.0" />
<PackageReference Include="FluentMigrator.Runner.Postgres" Version="5.0.0" />
```

## 11. API Documentation

```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Versioning" Version="5.1.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer" Version="5.1.0" />
```

## 12. File Storage (Cloud)

```xml
<!-- Azure Blob Storage -->
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />

<!-- AWS S3 -->
<PackageReference Include="AWSSDK.S3" Version="3.7.305.21" />
```

## 13. Notifications

```xml
<!-- Email -->
<PackageReference Include="MailKit" Version="4.3.0" />
<PackageReference Include="MimeKit" Version="4.3.0" />

<!-- SendGrid Alternative -->
<PackageReference Include="SendGrid" Version="9.29.2" />

<!-- SMS (Twilio) -->
<PackageReference Include="Twilio" Version="6.16.1" />
```

## 14. Validation & Guards

```xml
<PackageReference Include="FluentValidation" Version="11.9.0" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="Ardalis.GuardClauses" Version="4.5.0" />
```

## 15. MediatR (CQRS)

```xml
<PackageReference Include="MediatR" Version="12.2.0" />
<PackageReference Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="11.1.0" />
```

## 16. JSON & Serialization

```xml
<PackageReference Include="System.Text.Json" Version="8.0.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

## 17. HTTP Client

```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
```

## 18. Testing (Optional but Recommended)

```xml
<PackageReference Include="xUnit" Version="2.6.4" />
<PackageReference Include="xUnit.runner.visualstudio" Version="2.5.6" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.6.0" />
<PackageReference Include="Testcontainers.Redis" Version="3.6.0" />
<PackageReference Include="Testcontainers.RabbitMq" Version="3.6.0" />
```

## Installation Commands

### For a complete BahyWay project, install all packages:

```bash
# Observability
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Seq
dotnet add package Serilog.Exceptions
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Thread
dotnet add package Serilog.Formatting.Compact

# Health Checks
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks
dotnet add package AspNetCore.HealthChecks.UI
dotnet add package AspNetCore.HealthChecks.UI.Client
dotnet add package AspNetCore.HealthChecks.Npgsql
dotnet add package AspNetCore.HealthChecks.Redis

# Background Jobs
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.PostgreSql

# Caching
dotnet add package Microsoft.Extensions.Caching.Memory
dotnet add package StackExchange.Redis

# Resiliency
dotnet add package Polly
dotnet add package Polly.Extensions.Http

# Event Bus
dotnet add package MassTransit.RabbitMQ

# Entity Framework
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite

# API
dotnet add package Swashbuckle.AspNetCore

# Others
dotnet add package MediatR
dotnet add package FluentValidation.AspNetCore
```

## Package Versions

All package versions listed are for **.NET 8.0** compatibility.
Update versions as newer stable releases become available.

## Platform-Specific Notes

### Linux (Debian 12)
- All packages are fully compatible with Linux
- Ensure libicu is installed for PostgreSQL: `apt-get install libicu-dev`
- For Redis: `apt-get install redis-server`

### Windows
- All packages work without additional dependencies
- Consider using Windows Services for hosting

## Optional Performance Packages

```xml
<!-- For high-performance scenarios -->
<PackageReference Include="System.Threading.Channels" Version="8.0.0" />
<PackageReference Include="System.IO.Pipelines" Version="8.0.0" />
<PackageReference Include="BenchmarkDotNet" Version="0.13.11" />
```
