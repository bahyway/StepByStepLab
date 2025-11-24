# BahyWay PostgreSQL Replication Automation Module

## 🚀 Overview

Complete PowerShell automation module for managing PostgreSQL High Availability (HA) clusters with streaming replication. This module provides one-command setup and management of PostgreSQL primary-replica clusters for BahyWay projects.

## ✨ Features

- **One-Command Startup**: Start complete PostgreSQL HA cluster with single command
- **Automatic Replication**: Primary-replica streaming replication configured automatically
- **Health Monitoring**: Built-in health checks and replication lag monitoring
- **Easy Management**: Simple commands to start, stop, restart, and monitor cluster
- **Integrated Testing**: Comprehensive replication testing built-in
- **AlarmInsight API Integration**: Ready-to-use script for running AlarmInsight API
- **Cross-Platform**: Works on Windows, Linux, and macOS with PowerShell 7+
- **Data Persistence**: Docker volumes ensure data survives container restarts
- **PgAdmin Included**: Web-based database management interface

## 📋 Prerequisites

- **PowerShell 7.0+** - [Download](https://github.com/PowerShell/PowerShell/releases)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download) (for running AlarmInsight API)

### Verify Prerequisites

```powershell
# Check PowerShell version
$PSVersionTable.PSVersion  # Should be 7.0 or higher

# Check Docker
docker --version
docker-compose --version

# Check .NET SDK
dotnet --version  # Should be 8.0 or higher
```

## 🎯 Quick Start (30 seconds)

### Option 1: Run Everything (Recommended)

```powershell
# Navigate to automation module directory
cd infrastructure/postgresql-ha/automation-module

# Run AlarmInsight API with PostgreSQL cluster
./Start-AlarmInsightAPI.ps1
```

This single command will:
1. ✅ Check all prerequisites
2. ✅ Start PostgreSQL primary and replica containers
3. ✅ Create databases and users
4. ✅ Configure streaming replication
5. ✅ Test replication
6. ✅ Start AlarmInsight API with Hangfire

### Option 2: Manual Step-by-Step

```powershell
# 1. Import the module
Import-Module ./BahyWay.PostgreSQLReplication

# 2. Initialize and start cluster
Initialize-PostgreSQLCluster
Start-PostgreSQLCluster -Wait

# 3. Test replication
Test-PostgreSQLReplication

# 4. Check status
Get-PostgreSQLClusterStatus
```

## 📦 Module Installation

### Option 1: Direct Import (Recommended for Development)

```powershell
# From automation-module directory
Import-Module ./BahyWay.PostgreSQLReplication
```

### Option 2: Install to PowerShell Modules Path

```powershell
# Copy to user modules directory
$modulePath = "$HOME/Documents/PowerShell/Modules/BahyWay.PostgreSQLReplication"
Copy-Item -Recurse ./BahyWay.PostgreSQLReplication $modulePath

# Import module
Import-Module BahyWay.PostgreSQLReplication
```

## 🎮 Module Commands

### Core Management Commands

| Command | Alias | Description |
|---------|-------|-------------|
| `Initialize-PostgreSQLCluster` | - | Initialize cluster environment |
| `Start-PostgreSQLCluster` | `pgstart` | Start PostgreSQL cluster |
| `Stop-PostgreSQLCluster` | `pgstop` | Stop PostgreSQL cluster |
| `Restart-PostgreSQLCluster` | `pgrestart` | Restart PostgreSQL cluster |
| `Remove-PostgreSQLCluster` | - | Remove cluster completely |
| `Get-PostgreSQLClusterStatus` | `pgstatus` | Get cluster status |
| `Show-PostgreSQLClusterLogs` | `pglogs` | Show container logs |

### Monitoring & Testing Commands

| Command | Alias | Description |
|---------|-------|-------------|
| `Test-PostgreSQLReplication` | `pgtest` | Test replication |
| `Get-PostgreSQLReplicationLag` | - | Get replication lag metrics |

### Database Access Commands

| Command | Description |
|---------|-------------|
| `Connect-PostgreSQLPrimary` | Connect to primary (read-write) |
| `Connect-PostgreSQLReplica` | Connect to replica (read-only) |
| `Invoke-PostgreSQLQuery` | Execute SQL query |

### Backup & Restore Commands

| Command | Description |
|---------|-------------|
| `Backup-PostgreSQLCluster` | Backup database |
| `Restore-PostgreSQLCluster` | Restore from backup |

## 📚 Detailed Usage

### Starting the Cluster

```powershell
# Basic start
Start-PostgreSQLCluster

# Start and wait for healthy status
Start-PostgreSQLCluster -Wait

# Start with custom timeout
Start-PostgreSQLCluster -Wait -Timeout 180
```

### Checking Status

```powershell
# Full status with console output
Get-PostgreSQLClusterStatus

# Quiet mode (returns object only)
$status = Get-PostgreSQLClusterStatus -Quiet
Write-Host "Primary: $($status.Primary.State)"
Write-Host "Replica: $($status.Replica.State)"
```

### Viewing Logs

```powershell
# View all logs (last 100 lines)
Show-PostgreSQLClusterLogs

# View primary logs only
Show-PostgreSQLClusterLogs -Service Primary

# Follow logs (like tail -f)
Show-PostgreSQLClusterLogs -Service Primary -Follow

# View last 50 lines
Show-PostgreSQLClusterLogs -Tail 50
```

### Testing Replication

```powershell
# Run comprehensive replication test
Test-PostgreSQLReplication

# This will:
# - Check container status
# - Verify replication is streaming
# - Check replication lag
# - Perform write/read test
```

### Monitoring Replication Lag

```powershell
# Get replication lag metrics
$lag = Get-PostgreSQLReplicationLag

Write-Host "Lag: $($lag.LagBytes) bytes, $($lag.LagSeconds) seconds"
Write-Host "State: $($lag.State)"
```

### Connecting to Databases

```powershell
# Connect to primary (interactive psql)
Connect-PostgreSQLPrimary

# Connect to specific database
Connect-PostgreSQLPrimary -Database alarminsight

# Connect to replica (read-only)
Connect-PostgreSQLReplica -Database alarminsight_hangfire
```

### Executing SQL Queries

```powershell
# Execute query on primary
Invoke-PostgreSQLQuery -Query "SELECT version();"

# Execute on replica
Invoke-PostgreSQLQuery -Query "SELECT * FROM pg_stat_replication;" -Node Replica

# Execute on specific database
Invoke-PostgreSQLQuery -Query "SELECT * FROM users;" -Database alarminsight -Node Primary
```

### Backup and Restore

```powershell
# Backup all databases
Backup-PostgreSQLCluster -BackupPath "C:\Backups"

# Backup specific database
Backup-PostgreSQLCluster -BackupPath "/backups" -Database alarminsight

# Restore database
Restore-PostgreSQLCluster -BackupFile "C:\Backups\postgres_backup.sql" -Database alarminsight
```

### Stopping the Cluster

```powershell
# Stop cluster (preserve data)
Stop-PostgreSQLCluster

# Stop and remove volumes (WARNING: Deletes all data!)
Stop-PostgreSQLCluster -RemoveVolumes
```

### Removing the Cluster

```powershell
# Remove completely (with confirmation)
Remove-PostgreSQLCluster

# Force remove without confirmation
Remove-PostgreSQLCluster -Force
```

## 🔧 Configuration

### Connection Information

| Service | Host | Port | Default Credentials |
|---------|------|------|---------------------|
| **Primary** | localhost | 5432 | postgres / postgres_admin_pass |
| **Replica** | localhost | 5433 | postgres / postgres_admin_pass |
| **PgAdmin** | localhost | 5050 | admin@bahyway.com / admin |

### Databases

| Database | User | Password | Purpose |
|----------|------|----------|---------|
| `alarminsight_hangfire` | hangfire_user | hangfire_pass | Hangfire job storage |
| `alarminsight` | alarminsight_user | alarminsight_pass | Application data |

### Docker Resources

| Resource | Name |
|----------|------|
| **Containers** | `bahyway-postgres-primary`<br>`bahyway-postgres-replica`<br>`bahyway-pgadmin` |
| **Network** | `bahyway-network` |
| **Volumes** | `bahyway_postgres_primary_data`<br>`bahyway_postgres_replica_data`<br>`bahyway_pgadmin_data` |

## 🏗️ Architecture

```
┌─────────────────────────────────────────┐
│         AlarmInsight API                │
│      (Port 5000/5001/etc.)              │
│                                         │
│  - REST API Endpoints                   │
│  - Hangfire Dashboard                   │
│  - Health Monitoring                    │
└──────────────┬──────────────────────────┘
               │
               │ Connects to both
               │
       ┌───────┴────────┐
       │                │
┌──────▼──────┐  ┌─────▼───────┐
│  Primary    │  │   Replica   │
│ PostgreSQL  │  │ PostgreSQL  │
│             │  │             │
│ Port: 5432  │  │ Port: 5433  │
│             │  │             │
│ Read/Write  │  │ Read-Only   │
└──────┬──────┘  └─────▲───────┘
       │                │
       │                │
       └────Streaming────┘
         Replication

┌─────────────────────────────────────────┐
│            PgAdmin 4                    │
│        (Port 5050)                      │
│   Web-based Database Management         │
└─────────────────────────────────────────┘
```

## 📖 Common Scenarios

### Scenario 1: Daily Development

```powershell
# Start everything in the morning
./Start-AlarmInsightAPI.ps1

# Work on your code...
# API is running on http://localhost:5000
# Press Ctrl+C when done with API

# Stop cluster at end of day
Stop-PostgreSQLCluster
```

### Scenario 2: Cluster Already Running

```powershell
# Skip cluster start if already running
./Start-AlarmInsightAPI.ps1 -SkipClusterStart

# Or manually check status and run API
Get-PostgreSQLClusterStatus
cd ../../../src/AlarmInsight.API
dotnet run
```

### Scenario 3: Fresh Start

```powershell
# Clean start - removes all data
./Start-AlarmInsightAPI.ps1 -Clean

# Or manually
Remove-PostgreSQLCluster -Force
./Start-AlarmInsightAPI.ps1
```

### Scenario 4: Troubleshooting

```powershell
# Check cluster status
Get-PostgreSQLClusterStatus

# View logs
Show-PostgreSQLClusterLogs -Follow

# Test replication
Test-PostgreSQLReplication

# Check replication lag
Get-PostgreSQLReplicationLag

# Restart cluster
Restart-PostgreSQLCluster -Wait
```

### Scenario 5: Running API on Different Port

```powershell
# Run on port 5001
./Start-AlarmInsightAPI.ps1 -ApiPort 5001
```

## 🔍 Accessing Services

### AlarmInsight API

```bash
# API Base URL
http://localhost:5000

# Swagger UI (API Documentation)
http://localhost:5000/swagger

# Hangfire Dashboard
http://localhost:5000/hangfire

# Health Endpoints
http://localhost:5000/api/postgresql/health
http://localhost:5000/api/postgresql/healthz
http://localhost:5000/api/postgresql/replication
```

### PgAdmin

```bash
# Web URL
http://localhost:5050

# Credentials
Email: admin@bahyway.com
Password: admin
```

To add servers in PgAdmin:

**Primary Server:**
- Host: postgres-primary
- Port: 5432
- Username: postgres
- Password: postgres_admin_pass

**Replica Server:**
- Host: postgres-replica
- Port: 5432
- Username: postgres
- Password: postgres_admin_pass

### Database Clients

```bash
# Using psql from host machine

# Connect to primary
psql -h localhost -p 5432 -U postgres -d alarminsight

# Connect to replica
psql -h localhost -p 5433 -U postgres -d alarminsight

# Using module commands
Connect-PostgreSQLPrimary -Database alarminsight
Connect-PostgreSQLReplica -Database alarminsight
```

## 🛠️ Troubleshooting

### Issue: Containers won't start

```powershell
# Check if Docker is running
docker ps

# Check logs for errors
Show-PostgreSQLClusterLogs

# Try clean start
Remove-PostgreSQLCluster -Force
Start-PostgreSQLCluster -Wait
```

### Issue: Replication not working

```powershell
# Check replication status
Test-PostgreSQLReplication

# Check replication lag
Get-PostgreSQLReplicationLag

# View replica logs
Show-PostgreSQLClusterLogs -Service Replica -Follow
```

### Issue: Port already in use

```powershell
# Check what's using the port
# Windows
netstat -ano | findstr :5432

# Linux/Mac
lsof -i :5432

# Stop conflicting service or change port in docker-compose.yml
```

### Issue: API can't connect to database

```powershell
# Verify cluster is running
Get-PostgreSQLClusterStatus

# Check if databases exist
Connect-PostgreSQLPrimary
# Then in psql:
\l  # List databases

# Test connection from API
Invoke-PostgreSQLQuery -Query "SELECT version();"
```

### Issue: Module commands not found

```powershell
# Re-import module
Remove-Module BahyWay.PostgreSQLReplication -ErrorAction SilentlyContinue
Import-Module ./BahyWay.PostgreSQLReplication -Force

# Check loaded commands
Get-Command -Module BahyWay.PostgreSQLReplication
```

## 📂 File Structure

```
infrastructure/postgresql-ha/
├── docker-compose.yml                          # Docker Compose configuration
├── config/
│   ├── primary/
│   │   ├── postgresql.conf                     # Primary PostgreSQL config
│   │   └── pg_hba.conf                         # Primary access control
│   └── replica/
│       └── postgresql.conf                     # Replica PostgreSQL config
├── init-scripts/
│   ├── primary/
│   │   ├── 01-init-replication.sh              # Create replication user
│   │   └── 02-create-databases.sql             # Create databases & users
│   └── replica/
│       └── 01-setup-replica.sh                 # Replica setup script
└── automation-module/
    ├── BahyWay.PostgreSQLReplication/
    │   ├── BahyWay.PostgreSQLReplication.psd1  # Module manifest
    │   └── BahyWay.PostgreSQLReplication.psm1  # Module implementation
    ├── Start-AlarmInsightAPI.ps1               # All-in-one startup script
    └── README.md                               # This file
```

## 🎓 Learning Resources

### Understanding Replication

```powershell
# View replication status on primary
Invoke-PostgreSQLQuery -Query "SELECT * FROM pg_stat_replication;" -Node Primary

# View replication status on replica
Invoke-PostgreSQLQuery -Query "SELECT pg_is_in_recovery();" -Node Replica

# Check WAL lag
Invoke-PostgreSQLQuery -Query "
SELECT
    client_addr,
    state,
    pg_wal_lsn_diff(pg_current_wal_lsn(), sent_lsn) AS lag_bytes
FROM pg_stat_replication;
" -Node Primary
```

### Monitoring Queries

```sql
-- Check database sizes
SELECT
    datname,
    pg_size_pretty(pg_database_size(datname)) AS size
FROM pg_database
WHERE datname NOT IN ('template0', 'template1');

-- Check active connections
SELECT
    datname,
    count(*) as connections
FROM pg_stat_activity
GROUP BY datname;

-- Check replication lag (on primary)
SELECT
    client_addr,
    state,
    pg_wal_lsn_diff(pg_current_wal_lsn(), sent_lsn) AS lag_bytes,
    EXTRACT(EPOCH FROM (NOW() - pg_last_xact_replay_timestamp())) AS lag_seconds
FROM pg_stat_replication;
```

## 🚦 Best Practices

### Development Workflow

1. **Start cluster once per day** - No need to stop/start repeatedly
2. **Use `-Wait` flag** - Ensures cluster is healthy before API starts
3. **Check status regularly** - Use `Get-PostgreSQLClusterStatus`
4. **Monitor replication lag** - Should be < 5 seconds
5. **Stop cluster when not coding** - Saves system resources

### Production Considerations

⚠️ **This module is designed for development/testing, not production!**

For production:
- Use dedicated PostgreSQL servers or managed services
- Implement proper backup strategies
- Configure monitoring and alerting
- Use strong passwords (not defaults)
- Configure SSL/TLS
- Implement proper network security
- Use production-grade connection pooling

## 📊 Performance Tips

### Docker Resources

Increase Docker resources for better performance:
- **Memory**: 4GB minimum, 8GB recommended
- **CPU**: 4 cores recommended
- **Disk**: 20GB minimum for data

### PostgreSQL Tuning

The module uses sensible defaults. For heavy workloads, consider tuning:

```conf
# In postgresql.conf
shared_buffers = 512MB          # 25% of RAM
effective_cache_size = 2GB      # 50-75% of RAM
work_mem = 32MB
maintenance_work_mem = 128MB
```

## 🤝 Contributing

### Reporting Issues

If you encounter issues:
1. Run `Get-PostgreSQLClusterStatus`
2. Capture logs: `Show-PostgreSQLClusterLogs > logs.txt`
3. Include PowerShell version: `$PSVersionTable.PSVersion`
4. Include Docker version: `docker --version`

### Feature Requests

Suggestions for improvements are welcome!

## 📝 Changelog

### Version 1.0.0 (2024)
- Initial release
- Docker Compose-based PostgreSQL HA cluster
- Complete PowerShell automation module
- AlarmInsight API integration script
- Comprehensive health monitoring
- Backup and restore capabilities

## 📄 License

Copyright (c) 2024 BahyWay. All rights reserved.

## 🙏 Acknowledgments

- PostgreSQL community
- Docker community
- PowerShell community
- Hangfire contributors

---

**Need Help?** Check the [Troubleshooting](#🛠️-troubleshooting) section or run `Get-Help <CommandName> -Full` for detailed command documentation.

**Ready to Start?** Run `./Start-AlarmInsightAPI.ps1` and you're good to go! 🚀
