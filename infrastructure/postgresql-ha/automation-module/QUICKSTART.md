# 🚀 Quick Start Guide - PostgreSQL Replication for AlarmInsight API

## ⚡ 30-Second Start

```powershell
# 1. Navigate to automation module
cd infrastructure/postgresql-ha/automation-module

# 2. Run everything
./Start-AlarmInsightAPI.ps1
```

**That's it!** The script will:
- ✅ Check prerequisites (Docker, .NET, PowerShell)
- ✅ Start PostgreSQL primary (port 5432)
- ✅ Start PostgreSQL replica (port 5433)
- ✅ Configure streaming replication
- ✅ Create databases (`alarminsight`, `alarminsight_hangfire`)
- ✅ Test replication
- ✅ Start AlarmInsight API on http://localhost:5000

## 🌐 Access Points

After startup completes:

| Service | URL | Credentials |
|---------|-----|-------------|
| **AlarmInsight API** | http://localhost:5000 | N/A |
| **Swagger UI** | http://localhost:5000/swagger | N/A |
| **Hangfire Dashboard** | http://localhost:5000/hangfire | N/A |
| **PgAdmin** | http://localhost:5050 | admin@bahyway.com / admin |
| **PostgreSQL Primary** | localhost:5432 | postgres / postgres_admin_pass |
| **PostgreSQL Replica** | localhost:5433 | postgres / postgres_admin_pass |

## 🎯 Common Commands

### Start Everything
```powershell
./Start-AlarmInsightAPI.ps1
```

### Clean Start (Delete All Data)
```powershell
./Start-AlarmInsightAPI.ps1 -Clean
```

### Stop PostgreSQL Cluster
```powershell
Import-Module ./BahyWay.PostgreSQLReplication
Stop-PostgreSQLCluster
```

### Check Cluster Status
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

## 🛠️ Prerequisites Check

Run these commands to verify you have everything installed:

```powershell
# PowerShell 7+
$PSVersionTable.PSVersion

# Docker & Docker Compose
docker --version
docker-compose --version

# .NET 8 SDK
dotnet --version
```

If any command fails, install the missing component:
- **PowerShell 7**: https://github.com/PowerShell/PowerShell/releases
- **Docker Desktop**: https://www.docker.com/products/docker-desktop
- **.NET 8 SDK**: https://dotnet.microsoft.com/download

## 📊 What Gets Created?

### Docker Containers
- `bahyway-postgres-primary` - Primary PostgreSQL (read-write)
- `bahyway-postgres-replica` - Replica PostgreSQL (read-only)
- `bahyway-pgadmin` - PgAdmin web interface

### Databases
- `alarminsight_hangfire` - Hangfire job storage
  - User: `hangfire_user` / Password: `hangfire_pass`
- `alarminsight` - Application database
  - User: `alarminsight_user` / Password: `alarminsight_pass`

### Docker Volumes
- `bahyway_postgres_primary_data` - Primary data (persisted)
- `bahyway_postgres_replica_data` - Replica data (persisted)
- `bahyway_pgadmin_data` - PgAdmin settings (persisted)

## 🔧 Troubleshooting

### Port Already in Use
```powershell
# Check what's using port 5432
netstat -ano | findstr :5432  # Windows
lsof -i :5432                 # Linux/Mac

# Either stop the conflicting service or edit docker-compose.yml
```

### Containers Won't Start
```powershell
# Check Docker is running
docker ps

# View logs
Import-Module ./BahyWay.PostgreSQLReplication
Show-PostgreSQLClusterLogs

# Try clean start
./Start-AlarmInsightAPI.ps1 -Clean
```

### API Won't Connect to Database
```powershell
# Verify cluster is healthy
Import-Module ./BahyWay.PostgreSQLReplication
Get-PostgreSQLClusterStatus

# Test replication
Test-PostgreSQLReplication
```

## 📖 Next Steps

- **Read full documentation**: [README.md](README.md)
- **Explore API endpoints**: http://localhost:5000/swagger
- **Monitor with PgAdmin**: http://localhost:5050
- **Check Hangfire jobs**: http://localhost:5000/hangfire

## 💡 Pro Tips

1. **Leave cluster running** - No need to stop/start daily
2. **Use clean start sparingly** - Only when you need fresh data
3. **Monitor replication lag** - Should stay under 5 seconds
4. **Check logs for issues** - `Show-PostgreSQLClusterLogs`
5. **Backup important data** - Use `Backup-PostgreSQLCluster`

## 🎓 Learn More

```powershell
# Get help for any command
Get-Help Start-PostgreSQLCluster -Full
Get-Help Test-PostgreSQLReplication -Full

# List all available commands
Get-Command -Module BahyWay.PostgreSQLReplication
```

## 🚦 Daily Workflow

### Morning (Start Work)
```powershell
cd infrastructure/postgresql-ha/automation-module
./Start-AlarmInsightAPI.ps1
```

### During Work
- API runs on http://localhost:5000
- Make code changes as needed
- API auto-reloads on file changes (hot reload)
- Press `Ctrl+C` to stop API when needed

### Evening (End Work)
```powershell
# Stop API (Ctrl+C)
# Optionally stop cluster to save resources
Import-Module ./BahyWay.PostgreSQLReplication
Stop-PostgreSQLCluster
```

### Next Day
```powershell
# Cluster already has your data if you didn't stop it
# Just restart if needed
./Start-AlarmInsightAPI.ps1
```

---

**Questions?** Check the full [README.md](README.md) for detailed documentation!

**Happy Coding! 🎉**
