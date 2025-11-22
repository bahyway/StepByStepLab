# BahyWay PostgreSQL HA Management Module

Enterprise-grade PowerShell module for managing PostgreSQL High Availability clusters with comprehensive health checks, monitoring, and alarm detection.

## 🎯 Features

- ✅ **Cross-platform**: Works on Windows WSL2 and Linux
- ✅ **Comprehensive Health Checks**: Docker, PostgreSQL Primary/Replica, Replication, Storage
- ✅ **Alarm Detection**: Automatic detection and logging of issues
- ✅ **Try-Catch-Finally**: Robust error handling
- ✅ **Detailed Logging**: Structured logs with rotation
- ✅ **Ansible Deployment**: Infrastructure as Code
- ✅ **Automatic Monitoring**: Systemd timers for periodic checks
- ✅ **HAProxy & Barman Support**: Optional load balancer and backup monitoring

## 📦 Installation

### Method 1: Ansible Deployment (Recommended for Production)

```bash
# 1. Install Ansible
sudo apt install ansible -y  # Ubuntu/Debian
# or
brew install ansible  # MacOS

# 2. Clone repository
git clone https://github.com/bahyway/postgresql-ha-module.git
cd postgresql-ha-module

# 3. Edit inventory
nano inventory.yml

# 4. Deploy module
ansible-playbook -i inventory.yml deploy-postgresql-ha-module.yml

# 5. Verify installation
pwsh
Import-Module BahyWay.PostgreSQLHA
Get-Module BahyWay.PostgreSQLHA
```

### Method 2: Manual PowerShell Installation (Windows/WSL2)

```powershell
# 1. Download module files
git clone https://github.com/bahyway/postgresql-ha-module.git
cd postgresql-ha-module

# 2. Run installation script
.\Install-Module.ps1

# 3. Import module
Import-Module BahyWay.PostgreSQLHA

# 4. Verify
Get-Command -Module BahyWay.PostgreSQLHA
```

### Method 3: PowerShell Gallery (Coming Soon)

```powershell
Install-Module -Name BahyWay.PostgreSQLHA
Import-Module BahyWay.PostgreSQLHA
```

## 🚀 Quick Start

### Basic Health Check

```powershell
# Import module
Import-Module BahyWay.PostgreSQLHA

# Run comprehensive health check
Get-ClusterHealth

# Check specific components
Test-DockerEnvironment
Test-PostgreSQLPrimary
Test-PostgreSQLReplica
Test-PostgreSQLReplication
Test-StorageSpace
```

### Continuous Monitoring

```powershell
# Watch cluster health in real-time (updates every 5 seconds)
Watch-ClusterHealth

# Get replication status
Get-ReplicationStatus

# Check replication lag
Get-ReplicationLag
```

### View Logs and Alarms

```powershell
# Get module logs
Get-ModuleLog -Last 50

# Get health alarms
Get-HealthAlarms

# Get critical alarms only
Get-HealthAlarms -Severity Critical

# Export logs
Export-ModuleLogs -Path "./logs-export"
```

## 📊 Example Output

### Get-ClusterHealth

```
╔════════════════════════════════════════════════════════════╗
║          CLUSTER HEALTH SUMMARY                            ║
╚════════════════════════════════════════════════════════════╝

✅ ALL SYSTEMS OPERATIONAL

📊 Component Status:
   Docker:       ✅
   Primary:      ✅
   Replica:      ✅
   Replication:  ✅
   Storage:      ✅

📈 Metrics:
   Replication Lag:     2s
   Active Connections:  15
   Available Storage:   120GB
```

## 🔧 Configuration

### Default Paths

| Path | Linux/WSL2 | Windows |
|------|------------|---------|
| **Logs** | `/var/log/bahyway/postgresql-ha` | `C:\ProgramData\BahyWay\PostgreSQLHA\Logs` |
| **Alarms** | `/var/log/bahyway/postgresql-ha/alarms` | `C:\ProgramData\BahyWay\PostgreSQLHA\Alarms` |
| **Config** | `/etc/bahyway/postgresql-ha` | `C:\ProgramData\BahyWay\PostgreSQLHA\Config` |

### Module Configuration

Edit `/etc/bahyway/postgresql-ha/config.json`:

```json
{
  "docker": {
    "primaryContainerName": "bahyway-postgres-primary",
    "replicaContainerName": "bahyway-postgres-replica",
    "networkName": "bahyway-network"
  },
  "thresholds": {
    "minimumDiskSpaceGB": 50,
    "replicationLagThresholdSeconds": 5,
    "alarmRetentionDays": 30
  },
  "monitoring": {
    "healthCheckIntervalMinutes": 5,
    "enableAutomaticAlerts": true,
    "alertEmail": "your-email@company.com"
  }
}
```

### Update Configuration

```powershell
# Get current configuration
Get-ClusterConfiguration

# Set configuration value
Set-ClusterConfiguration -Key "thresholds.minimumDiskSpaceGB" -Value 100

# Export configuration
Export-ClusterConfiguration -Path "./backup-config.json"

# Import configuration
Import-ClusterConfiguration -Path "./backup-config.json"
```

## 🔍 Health Check Details

### Test-DockerEnvironment

Checks:
- ✅ Docker command availability
- ✅ Docker version
- ✅ Docker daemon running status
- ✅ Platform detection (Linux/Windows/MacOS/WSL2)
- ✅ Container count and images

**Alarms Generated:**
- `DockerNotInstalled` (Critical)
- `DockerNotRunning` (Critical)

### Test-PostgreSQLPrimary

Checks:
- ✅ Container exists and running
- ✅ PostgreSQL accepting connections
- ✅ Recovery mode status (should be FALSE)
- ✅ Replication configuration
- ✅ Active connections count
- ✅ Database size

**Alarms Generated:**
- `PrimaryContainerMissing` (Critical)
- `PrimaryContainerNotRunning` (Critical)
- `PrimaryDatabaseNotResponding` (Critical)
- `PrimaryInRecoveryMode` (Critical)
- `NoReplicasConnected` (High)

### Test-PostgreSQLReplica

Checks:
- ✅ Container exists and running
- ✅ PostgreSQL accepting connections
- ✅ Recovery mode status (should be TRUE)
- ✅ Replication lag
- ✅ WAL replay status

**Alarms Generated:**
- `ReplicaContainerMissing` (High)
- `ReplicaContainerNotRunning` (High)
- `ReplicaDatabaseNotResponding` (High)
- `ReplicaNotInRecoveryMode` (Critical)
- `HighReplicationLag` (Medium)

### Test-PostgreSQLReplication

Checks:
- ✅ Replica connected to primary
- ✅ Streaming state
- ✅ Sync state (async/sync)
- ✅ Write/Flush/Replay lag
- ✅ Replication slots

**Alarms Generated:**
- `NoReplicationConnection` (Critical)
- `ReplicationNotStreaming` (High)

### Test-StorageSpace

Checks:
- ✅ Host available storage
- ✅ Storage used percentage
- ✅ Docker volumes
- ✅ Minimum space threshold

**Alarms Generated:**
- `LowDiskSpace` (High)

## 📋 Available Functions

### Health Check Functions
- `Test-DockerEnvironment` - Docker availability
- `Test-PostgreSQLPrimary` - Primary node health
- `Test-PostgreSQLReplica` - Replica node health
- `Test-PostgreSQLReplication` - Replication status
- `Test-HAProxyHealth` - HAProxy load balancer
- `Test-BarmanBackup` - Barman backup system
- `Test-StorageSpace` - Disk space availability
- `Get-ClusterHealth` - Comprehensive cluster check

### Monitoring Functions
- `Get-ReplicationStatus` - Detailed replication info
- `Get-ReplicationLag` - Lag metrics
- `Get-DatabaseSize` - Database sizes
- `Get-ConnectionCount` - Active connections
- `Watch-ClusterHealth` - Real-time monitoring

### Maintenance Functions
- `Start-PostgreSQLCluster` - Start cluster
- `Stop-PostgreSQLCluster` - Stop cluster
- `Restart-PostgreSQLNode` - Restart specific node
- `Invoke-FailoverToReplica` - Manual failover
- `Invoke-BaseBackup` - Create base backup

### Alarm Functions
- `Send-HealthAlarm` - Manual alarm creation
- `Get-HealthAlarms` - View alarms
- `Clear-HealthAlarms` - Clear old alarms

### Configuration Functions
- `Get-ClusterConfiguration` - View config
- `Set-ClusterConfiguration` - Update config
- `Export-ClusterConfiguration` - Export config
- `Import-ClusterConfiguration` - Import config

### Log Functions
- `Get-ModuleLog` - View logs
- `Clear-ModuleLog` - Clear logs
- `Export-ModuleLogs` - Export logs

## 🔔 Alarm System

### Alarm Types

| Alarm Type | Severity | Description |
|------------|----------|-------------|
| `DockerNotInstalled` | Critical | Docker not found |
| `DockerNotRunning` | Critical | Docker daemon not running |
| `PrimaryContainerMissing` | Critical | Primary container doesn't exist |
| `PrimaryContainerNotRunning` | Critical | Primary container stopped |
| `PrimaryDatabaseNotResponding` | Critical | PostgreSQL not accepting connections |
| `NoReplicationConnection` | Critical | No replica connected |
| `ReplicaNotInRecoveryMode` | Critical | Replica not in standby mode |
| `NoReplicasConnected` | High | No replicas streaming |
| `ReplicationNotStreaming` | High | Replication not in streaming state |
| `HighReplicationLag` | Medium | Lag exceeds threshold |
| `LowDiskSpace` | High | Disk space below minimum |

### Alarm Log Format

```json
{
  "Timestamp": "2025-11-22T15:30:45Z",
  "AlarmType": "HighReplicationLag",
  "Severity": "Medium",
  "Component": "PostgreSQL-Replica",
  "Message": "Replication lag is 10 seconds (threshold: 5)",
  "Details": {
    "LagSeconds": 10
  },
  "Acknowledged": false
}
```

### View Alarms

```powershell
# Get all alarms
Get-HealthAlarms

# Get critical alarms
Get-HealthAlarms -Severity Critical

# Get alarms from last 24 hours
Get-HealthAlarms -Since (Get-Date).AddDays(-1)

# Get alarms for specific component
Get-HealthAlarms -Component "PostgreSQL-Replica"
```

## 🔄 Automatic Monitoring

The Ansible deployment sets up automatic monitoring via systemd:

```bash
# Check timer status
systemctl status postgresql-ha-healthcheck.timer

# View automatic health check logs
journalctl -u postgresql-ha-healthcheck.service -f

# Manual trigger
systemctl start postgresql-ha-healthcheck.service

# Disable automatic checks
systemctl stop postgresql-ha-healthcheck.timer
systemctl disable postgresql-ha-healthcheck.timer
```

## 🐛 Troubleshooting

### Module Not Found

```powershell
# Check module paths
$env:PSModulePath -split [System.IO.Path]::PathSeparator

# Add module path (Linux)
$env:PSModulePath += ":/usr/local/share/powershell/Modules"

# Reload module
Remove-Module BahyWay.PostgreSQLHA -ErrorAction SilentlyContinue
Import-Module BahyWay.PostgreSQLHA
```

### Permission Denied

```bash
# Fix log directory permissions
sudo chown -R $USER:$USER /var/log/bahyway
sudo chmod -R 755 /var/log/bahyway

# Fix Docker socket permissions (Linux)
sudo usermod -aG docker $USER
newgrp docker
```

### Docker Connection Issues

```powershell
# Test Docker
docker ps

# Check Docker service (Linux)
sudo systemctl status docker

# Check Docker context
docker context ls
```

## 📚 Integration with AlarmInsight

This module is designed to integrate with the BahyWay AlarmInsight system:

```powershell
# Get cluster health
$health = Get-ClusterHealth

# Send to AlarmInsight API
if (-not $health.IsHealthy) {
    foreach ($alarm in (Get-HealthAlarms -Severity Critical)) {
        Invoke-RestMethod -Uri "http://alarminsight-api/api/alarms" `
            -Method POST `
            -Body ($alarm | ConvertTo-Json) `
            -ContentType "application/json"
    }
}
```

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## 📄 License

Copyright © 2025 BahyWay Solutions. All rights reserved.

## 📞 Support

- **Email**: support@bahyway.com
- **GitHub Issues**: https://github.com/bahyway/postgresql-ha-module/issues
- **Documentation**: https://docs.bahyway.com/postgresql-ha

## 🎯 Roadmap

- [ ] PowerShell Gallery publication
- [ ] Email/SMS notification integration
- [ ] Grafana dashboard export
- [ ] Prometheus metrics export
- [ ] Automated failover logic
- [ ] Multi-datacenter support
- [ ] Kubernetes operator version

---

**Built with ❤️ by BahyWay Solutions**
