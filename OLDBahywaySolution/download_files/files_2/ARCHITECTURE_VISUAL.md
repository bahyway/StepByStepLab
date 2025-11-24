# BahyWay Architecture - Visual Component Map

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         BahyWay Ecosystem Architecture                       │
│                        (Implemented Components - Week 1-2)                   │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                            PRESENTATION LAYER                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  Blazor Web  │  │  React/HTML  │  │   REST API   │  │   Swagger    │  │
│  │     (UI)     │  │   (bahyway   │  │  Endpoints   │  │     Docs     │  │
│  │              │  │     .com)    │  │              │  │              │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          MIDDLEWARE PIPELINE ✅                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  1. CorrelationIdMiddleware      → Adds tracking ID to requests      │  │
│  │  2. RequestLoggingMiddleware     → Logs HTTP requests/responses      │  │
│  │  3. MetricsMiddleware             → Collects performance metrics     │  │
│  │  4. ExceptionHandlingMiddleware   → Catches and logs errors          │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          APPLICATION LAYER                                   │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                      MediatR Command/Query Handlers                   │  │
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐        │  │
│  │  │   Commands     │  │    Queries     │  │  Domain Events │        │  │
│  │  │ (Write Ops)    │  │  (Read Ops)    │  │   Publishers   │        │  │
│  │  └────────────────┘  └────────────────┘  └────────────────┘        │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            DOMAIN LAYER                                      │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │  │
│  │  │   Entities   │  │    Value     │  │  Aggregates  │              │  │
│  │  │              │  │   Objects    │  │     Root     │              │  │
│  │  └──────────────┘  └──────────────┘  └──────────────┘              │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │  │
│  │  │    Domain    │  │ Domain Logic │  │ Rules Engine │              │  │
│  │  │    Events    │  │   Services   │  │   (Rust)     │              │  │
│  │  └──────────────┘  └──────────────┘  └──────────────┘              │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        INFRASTRUCTURE LAYER ✅                               │
│                                                                               │
│  ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓  │
│  ┃              OBSERVABILITY INFRASTRUCTURE ✅                          ┃  │
│  ┃  ┌────────────────────────────────────────────────────────────────┐ ┃  │
│  ┃  │ Logging (Serilog) ✅                                           │ ┃  │
│  ┃  │  • Structured logging with context                            │ ┃  │
│  ┃  │  • Multiple sinks: Console, File, Seq, Elasticsearch          │ ┃  │
│  ┃  │  • Correlation ID propagation                                 │ ┃  │
│  ┃  │  • Request/Response logging                                   │ ┃  │
│  ┃  └────────────────────────────────────────────────────────────────┘ ┃  │
│  ┃  ┌────────────────────────────────────────────────────────────────┐ ┃  │
│  ┃  │ Health Checks ✅                                               │ ┃  │
│  ┃  │  • Database (PostgreSQL)                                       │ ┃  │
│  ┃  │  • Cache (Redis)                                               │ ┃  │
│  ┃  │  • Message Queue (RabbitMQ)                                    │ ┃  │
│  ┃  │  • File System                                                 │ ┃  │
│  ┃  │  • External APIs                                               │ ┃  │
│  ┃  └────────────────────────────────────────────────────────────────┘ ┃  │
│  ┃  ┌────────────────────────────────────────────────────────────────┐ ┃  │
│  ┃  │ Metrics (OpenTelemetry/Prometheus) ✅                          │ ┃  │
│  ┃  │  • HTTP request metrics                                        │ ┃  │
│  ┃  │  • Database query metrics                                      │ ┃  │
│  ┃  │  • Cache hit/miss metrics                                      │ ┃  │
│  ┃  │  • Background job metrics                                      │ ┃  │
│  ┃  │  • Business metrics (custom)                                   │ ┃  │
│  ┃  └────────────────────────────────────────────────────────────────┘ ┃  │
│  ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛  │
│                                                                               │
│  ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓  │
│  ┃              FILE WATCHER SERVICE ✅ (YOU IDENTIFIED THIS!)          ┃  │
│  ┃  ┌────────────────────────────────────────────────────────────────┐ ┃  │
│  ┃  │ FileWatcher Background Service ✅                              │ ┃  │
│  ┃  │  • Multi-directory monitoring                                  │ ┃  │
│  ┃  │  • Large file handling (wait until complete)                   │ ┃  │
│  ┃  │  • File lock detection                                         │ ┃  │
│  ┃  │  • Automatic ZIP extraction                                    │ ┃  │
│  ┃  │  • SHA256 integrity checking                                   │ ┃  │
│  ┃  │  • Error handling & retry logic                                │ ┃  │
│  ┃  │  • Processed/Error folder management                           │ ┃  │
│  ┃  └────────────────────────────────────────────────────────────────┘ ┃  │
│  ┃  ┌────────────────────────────────────────────────────────────────┐ ┃  │
│  ┃  │ Pre-configured Watch Folders:                                  │ ┃  │
│  ┃  │  → ETLway: CSV, Excel, ZIP imports                             │ ┃  │
│  ┃  │  → HireWay: Resume processing (PDF, DOCX)                      │ ┃  │
│  ┃  │  → SSISight: SSIS packages (.dtsx)                             │ ┃  │
│  ┃  │  → AlarmInsight: Historical alarm data                         │ ┃  │
│  ┃  │  → NajafCemetery: Photos & documents                           │ ┃  │
│  ┃  └────────────────────────────────────────────────────────────────┘ ┃  │
│  ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛  │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Persistence (Already Implemented ✓)                                 │   │
│  │  • EF Core with PostgreSQL                                          │   │
│  │  • Repository Pattern                                               │   │
│  │  • Unit of Work Pattern                                             │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Rules Engine (Already Implemented ✓)                                │   │
│  │  • Rust-based high-performance engine                               │   │
│  │  • FFI integration with .NET                                        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Coming Next (Weeks 3-12) ⏳                                          │   │
│  │  • Background Jobs (Hangfire)                                       │   │
│  │  • Caching (Redis)                                                  │   │
│  │  • Event Bus (MassTransit + RabbitMQ)                               │   │
│  │  • Audit Logging                                                    │   │
│  │  • Resiliency (Polly)                                               │   │
│  │  • File Storage Abstraction                                         │   │
│  │  • Notification System                                              │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    EXTERNAL SYSTEMS & MONITORING                             │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐           │
│  │ PostgreSQL │  │   Redis    │  │  RabbitMQ  │  │   Seq      │           │
│  │  Database  │  │   Cache    │  │  Message   │  │    Logs    │           │
│  │            │  │            │  │   Queue    │  │            │           │
│  └────────────┘  └────────────┘  └────────────┘  └────────────┘           │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐           │
│  │ Prometheus │  │  Grafana   │  │Elasticsearch│ │File System│           │
│  │  Metrics   │  │Dashboards  │  │    Logs    │  │  Storage  │           │
│  │            │  │            │  │            │  │            │           │
│  └────────────┘  └────────────┘  └────────────┘  └────────────┘           │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                         DATA FLOW EXAMPLE                                    │
│                                                                               │
│  1. File Dropped in Watch Folder                                             │
│     │                                                                         │
│     ├──→ FileWatcher detects new file                                        │
│     │    ├──→ Wait for file to be fully written (no locks)                   │
│     │    ├──→ Calculate SHA256 hash                                          │
│     │    ├──→ Extract if ZIP file                                            │
│     │    └──→ Call IFileProcessor                                            │
│     │                                                                         │
│     └──→ IFileProcessor (your implementation)                                │
│          ├──→ Parse file content                                             │
│          ├──→ Send MediatR command                                           │
│          └──→ Record metrics                                                 │
│               │                                                               │
│               └──→ Command Handler                                           │
│                    ├──→ Domain logic & Rules Engine                          │
│                    ├──→ Save to database                                     │
│                    ├──→ Publish domain events                                │
│                    └──→ Return success                                       │
│                         │                                                    │
│                         └──→ FileWatcher moves file to processed folder      │
│                                                                               │
│  2. All Steps Logged with Correlation ID                                     │
│     └──→ Logs → Seq/Elasticsearch → Searchable                               │
│                                                                               │
│  3. All Steps Tracked with Metrics                                           │
│     └──→ Metrics → Prometheus → Grafana Dashboards                           │
│                                                                               │
│  4. Health Checks Monitor System                                             │
│     └──→ /health endpoint → Monitoring alerts                                │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                     BOUNDED CONTEXTS (Example: AlarmInsight)                 │
│                                                                               │
│  AlarmManagement Bounded Context                                             │
│  ├── Domain                                                                  │
│  │   ├── Alarm (Aggregate Root)                                              │
│  │   ├── AlarmSeverity (Value Object)                                        │
│  │   └── AlarmProcessingRules (Domain Service → Rust Engine)                 │
│  ├── Application                                                             │
│  │   ├── Commands: ProcessAlarmCommand, EscalateAlarmCommand                 │
│  │   ├── Queries: GetAlarmByIdQuery, GetAlarmsForLocationQuery              │
│  │   └── EventHandlers: AlarmProcessedEventHandler                           │
│  └── Infrastructure                                                          │
│      ├── AlarmRepository (EF Core)                                           │
│      ├── AlarmFileProcessor ✅ (IFileProcessor implementation)                │
│      ├── Logging ✅ (Serilog with correlation)                                │
│      ├── Metrics ✅ (Business metrics for alarms)                             │
│      └── HealthChecks ✅ (Database, dependencies)                             │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘

Legend:
✅ = Implemented (Weeks 1-2)
✓ = Already exists in your architecture
⏳ = Planned (Weeks 3-16)
```
