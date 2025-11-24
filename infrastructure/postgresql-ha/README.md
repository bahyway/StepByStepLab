# PostgreSQL High Availability (HA) Infrastructure

## 📁 Directory Structure

This directory contains everything needed for PostgreSQL HA replication setup:

```
postgresql-ha/
├── docker-compose.yml                    # 🐳 Docker Compose configuration
├── config/                               # ⚙️ PostgreSQL configurations
│   ├── primary/
│   │   ├── postgresql.conf              # Primary server config
│   │   └── pg_hba.conf                  # Primary access control
│   └── replica/
│       └── postgresql.conf              # Replica server config
├── init-scripts/                         # 🎬 Initialization scripts
│   ├── primary/
│   │   ├── 01-init-replication.sh       # Setup replication user
│   │   └── 02-create-databases.sql      # Create databases & users
│   └── replica/
│       └── 01-setup-replica.sh          # Replica initialization
├── automation-module/                    # 🚀 PowerShell automation
│   ├── BahyWay.PostgreSQLReplication/   # PowerShell module
│   │   ├── BahyWay.PostgreSQLReplication.psd1
│   │   └── BahyWay.PostgreSQLReplication.psm1
│   ├── Start-AlarmInsightAPI.ps1        # All-in-one startup script
│   ├── README.md                        # Full documentation
│   └── QUICKSTART.md                    # Quick start guide
└── powershell-module/                    # 📊 Health monitoring module
    └── BahyWay.PostgreSQLHA/            # Existing monitoring module
```

## 🚀 Quick Start

### Option 1: Automated Startup (Recommended)

Run everything with one command:

```powershell
cd automation-module
./Start-AlarmInsightAPI.ps1
```

This will:
1. ✅ Start PostgreSQL primary (port 5432)
2. ✅ Start PostgreSQL replica (port 5433)
3. ✅ Configure streaming replication
4. ✅ Create databases and users
5. ✅ Test replication
6. ✅ Start AlarmInsight API

### Option 2: Manual Docker Compose

```bash
# Start cluster
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f

# Stop cluster
docker-compose down
```

### Option 3: PowerShell Module

```powershell
cd automation-module
Import-Module ./BahyWay.PostgreSQLReplication

# Initialize and start
Initialize-PostgreSQLCluster
Start-PostgreSQLCluster -Wait

# Test replication
Test-PostgreSQLReplication

# Check status
Get-PostgreSQLClusterStatus
```

## 📚 Documentation

- **[Automation Module README](automation-module/README.md)** - Complete PowerShell module documentation
- **[Quick Start Guide](automation-module/QUICKSTART.md)** - Get started in 30 seconds
- **[Monitoring Module](powershell-module/README.md)** - Health monitoring module docs

## 🎯 What Gets Created

### Docker Containers

| Container | Purpose | Port |
|-----------|---------|------|
| `bahyway-postgres-primary` | Primary PostgreSQL (read-write) | 5432 |
| `bahyway-postgres-replica` | Replica PostgreSQL (read-only) | 5433 |
| `bahyway-pgadmin` | Web database manager | 5050 |

### Databases

| Database | User | Purpose |
|----------|------|---------|
| `alarminsight_hangfire` | `hangfire_user` | Hangfire job storage (HA) |
| `alarminsight` | `alarminsight_user` | Application data |

### Replication Architecture

```
┌─────────────────────────────────┐
│    AlarmInsight API             │
│    Hangfire Dashboard           │
└───────────┬─────────────────────┘
            │
            │ Connects to both
            │
    ┌───────┴────────┐
    │                │
┌───▼────┐      ┌───▼─────┐
│Primary │─────>│ Replica │
│Port:   │      │Port:    │
│5432    │      │5433     │
└────────┘      └─────────┘
  (R/W)         (Read-Only)
    │                │
    └──Streaming─────┘
      Replication
```

## 🔧 Configuration

### Connection Strings

```json
{
  "ConnectionStrings": {
    "HangfireConnection": "Host=localhost;Port=5432;Database=alarminsight_hangfire;Username=hangfire_user;Password=hangfire_pass",
    "AlarmInsightConnection": "Host=localhost;Port=5432;Database=alarminsight;Username=alarminsight_user;Password=alarminsight_pass"
  }
}
```

### Default Credentials

⚠️ **For development only! Change for production!**

- **Admin**: postgres / postgres_admin_pass
- **Replication**: replicator / replicator_pass
- **Hangfire**: hangfire_user / hangfire_pass
- **Application**: alarminsight_user / alarminsight_pass
- **PgAdmin**: admin@bahyway.com / admin

## 🌐 Access Services

| Service | URL | Credentials |
|---------|-----|-------------|
| **AlarmInsight API** | http://localhost:5000 | - |
| **Swagger UI** | http://localhost:5000/swagger | - |
| **Hangfire Dashboard** | http://localhost:5000/hangfire | - |
| **PgAdmin** | http://localhost:5050 | admin@bahyway.com / admin |
| **PostgreSQL Primary** | localhost:5432 | postgres / postgres_admin_pass |
| **PostgreSQL Replica** | localhost:5433 | postgres / postgres_admin_pass |

## 📦 Features

### Docker Compose Setup
- ✅ PostgreSQL 16 (Alpine Linux)
- ✅ Streaming replication (primary → replica)
- ✅ Automatic initialization scripts
- ✅ Persistent data volumes
- ✅ Health checks
- ✅ PgAdmin web interface
- ✅ Custom network isolation

### PowerShell Automation Module
- ✅ One-command cluster management
- ✅ Automatic health monitoring
- ✅ Replication testing
- ✅ Backup and restore
- ✅ Interactive database connections
- ✅ Comprehensive logging
- ✅ Cross-platform (Windows/Linux/macOS)

### Health Monitoring Module
- ✅ Docker environment testing
- ✅ Primary/replica health checks
- ✅ Replication status monitoring
- ✅ Lag detection (threshold: 5 seconds)
- ✅ Alarm system
- ✅ C# integration

## 🛠️ Common Operations

### Start Cluster
```powershell
cd automation-module
./Start-AlarmInsightAPI.ps1
```

### Stop Cluster
```powershell
Import-Module ./BahyWay.PostgreSQLReplication
Stop-PostgreSQLCluster
```

### Check Status
```powershell
Import-Module ./BahyWay.PostgreSQLReplication
Get-PostgreSQLClusterStatus
```

### View Logs
```powershell
Import-Module ./BahyWay.PostgreSQLReplication
Show-PostgreSQLClusterLogs -Follow
```

### Test Replication
```powershell
Import-Module ./BahyWay.PostgreSQLReplication
Test-PostgreSQLReplication
```

### Connect to Databases
```powershell
# Primary (read-write)
Import-Module ./BahyWay.PostgreSQLReplication
Connect-PostgreSQLPrimary -Database alarminsight

# Replica (read-only)
Connect-PostgreSQLReplica -Database alarminsight
```

### Backup
```powershell
Import-Module ./BahyWay.PostgreSQLReplication
Backup-PostgreSQLCluster -BackupPath "C:\Backups"
```

## 📖 Integration Examples

### Using with AlarmInsight API

The AlarmInsight API (`src/AlarmInsight.API`) is pre-configured to use this PostgreSQL cluster:

```powershell
# Start everything
cd infrastructure/postgresql-ha/automation-module
./Start-AlarmInsightAPI.ps1

# API will be available at:
# - http://localhost:5000
# - http://localhost:5000/swagger
# - http://localhost:5000/hangfire
```

### Using with Your Own Application

Update your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=alarminsight;Username=alarminsight_user;Password=alarminsight_pass"
  }
}
```

For read replicas:

```json
{
  "ConnectionStrings": {
    "WriteConnection": "Host=localhost;Port=5432;...",
    "ReadConnection": "Host=localhost;Port=5433;..."
  }
}
```

## 🔍 Monitoring

### Check Replication Status

```sql
-- On primary
SELECT * FROM pg_stat_replication;

-- On replica
SELECT pg_is_in_recovery();
```

### Check Replication Lag

```powershell
Import-Module ./BahyWay.PostgreSQLReplication
Get-PostgreSQLReplicationLag
```

### Health Endpoints

Using the existing monitoring module:

```powershell
cd powershell-module
Import-Module ./BahyWay.PostgreSQLHA

Test-DockerEnvironment
Test-PostgreSQLPrimary
Test-PostgreSQLReplica
Test-PostgreSQLReplication
Get-ClusterHealth
```

## 🐛 Troubleshooting

### Containers won't start
```bash
# Check Docker
docker ps

# View logs
docker-compose logs

# Clean restart
docker-compose down -v
docker-compose up -d
```

### Replication not working
```powershell
# Check status
Get-PostgreSQLClusterStatus

# Test replication
Test-PostgreSQLReplication

# View replica logs
Show-PostgreSQLClusterLogs -Service Replica -Follow
```

### Port conflicts
```bash
# Check ports
netstat -ano | findstr :5432  # Windows
lsof -i :5432                 # Linux/Mac

# Either stop conflicting service or change port in docker-compose.yml
```

## 📚 Additional Resources

### Documentation Files
- [Automation Module README](automation-module/README.md)
- [Quick Start Guide](automation-module/QUICKSTART.md)
- [Monitoring Module README](powershell-module/README.md)
- [Monitoring Module Quick Start](powershell-module/QUICKSTART.md)

### Related Project Documentation
- [BahyWay Developer Quick Reference](../../BahyWay-Developer-Quick-Reference.md)
- [PostgreSQL HA Integration Summary](../../POSTGRESQL_HA_INTEGRATION_SUMMARY.md)
- [Hangfire Fix Solution](../../HANGFIRE_FIX_SOLUTION.md)

## ⚠️ Important Notes

1. **Development Only**: Default credentials are for development. Change for production!
2. **Data Persistence**: Docker volumes persist data across container restarts
3. **Clean Start**: Use `-Clean` flag to remove all data and start fresh
4. **Monitoring**: Regular replication lag monitoring recommended (< 5 seconds)
5. **Backups**: Implement regular backup strategy for production

## 🤝 Support

For issues or questions:
1. Check [Troubleshooting](#🐛-troubleshooting)
2. Review [Automation Module README](automation-module/README.md)
3. Check logs: `Show-PostgreSQLClusterLogs`
4. Test replication: `Test-PostgreSQLReplication`

## 📝 Version

- **PostgreSQL**: 16 (Alpine)
- **Automation Module**: 1.0.0
- **Monitoring Module**: 1.0.0
- **Docker Compose**: 3.8

---

**Ready to get started?** Run `cd automation-module && ./Start-AlarmInsightAPI.ps1` 🚀
