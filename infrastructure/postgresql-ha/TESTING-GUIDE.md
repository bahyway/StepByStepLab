# PostgreSQL Replication Testing Guide

## Overview

This guide provides comprehensive testing procedures for the BahyWay PostgreSQL High Availability (HA) replication setup. Follow these tests to verify that the replication cluster is working correctly.

## Prerequisites

Before running tests, ensure you have:

- **Docker Desktop** installed and running
- **PowerShell 7+** installed
- **Network access** to ports 5432, 5433, and 5050
- **Sufficient resources**: 4GB RAM, 4 CPU cores recommended

### Verify Prerequisites

```bash
# Check Docker
docker --version
docker-compose --version

# Check PowerShell (should be 7.0+)
pwsh --version

# Check available resources
docker system info | grep -E "CPUs|Total Memory"
```

## Test Environment Setup

### 1. Navigate to Automation Module

```powershell
cd infrastructure/postgresql-ha/automation-module
```

### 2. Import PowerShell Module

```powershell
Import-Module ./BahyWay.PostgreSQLReplication
```

Expected output:
```
BahyWay PostgreSQL Replication Module loaded successfully!
Run 'Get-Command -Module BahyWay.PostgreSQLReplication' to see available commands
```

## Test Procedures

### Test 1: Module Commands Available

**Purpose**: Verify all module commands are loaded

```powershell
Get-Command -Module BahyWay.PostgreSQLReplication
```

**Expected Result**: Should show 14 functions:
- Initialize-PostgreSQLCluster
- Start-PostgreSQLCluster (alias: pgstart)
- Stop-PostgreSQLCluster (alias: pgstop)
- Restart-PostgreSQLCluster (alias: pgrestart)
- Remove-PostgreSQLCluster
- Get-PostgreSQLClusterStatus (alias: pgstatus)
- Show-PostgreSQLClusterLogs (alias: pglogs)
- Test-PostgreSQLReplication (alias: pgtest)
- Get-PostgreSQLReplicationLag
- Connect-PostgreSQLPrimary
- Connect-PostgreSQLReplica
- Invoke-PostgreSQLQuery
- Backup-PostgreSQLCluster
- Restore-PostgreSQLCluster

**Status**: ✓ PASS / ✗ FAIL

---

### Test 2: Initialize Cluster Environment

**Purpose**: Verify environment initialization and prerequisites

```powershell
Initialize-PostgreSQLCluster
```

**Expected Result**:
```
==================================================
Initializing PostgreSQL HA Cluster Environment
==================================================
✓ Docker is available
✓ Docker Compose file found: /path/to/docker-compose.yml
✓ Created log directory: /path/to/logs
✓ Docker Compose configuration is valid
==================================================
Initialization complete!
==================================================
```

**Status**: ✓ PASS / ✗ FAIL

---

### Test 3: Start PostgreSQL Cluster

**Purpose**: Start the cluster and wait for healthy status

```powershell
Start-PostgreSQLCluster -Wait -Timeout 180
```

**Expected Result**:
- Containers start successfully
- Health checks pass within timeout period
- Connection information displayed
- No error messages

**Verify containers are running**:
```bash
docker ps --filter "name=bahyway-postgres"
```

Should show 3 containers:
- bahyway-postgres-primary (port 5432)
- bahyway-postgres-replica (port 5433)
- bahyway-pgadmin (port 5050)

**Status**: ✓ PASS / ✗ FAIL

**Time to Healthy**: _____ seconds (should be < 180s)

---

### Test 4: Cluster Status Check

**Purpose**: Verify cluster status reporting

```powershell
Get-PostgreSQLClusterStatus
```

**Expected Result**:
```
==================================================
PostgreSQL HA Cluster Status
==================================================

Primary Node:
  Container: bahyway-postgres-primary
  State:     running
  Health:    healthy
  Port:      localhost:5432

Replica Node:
  Container: bahyway-postgres-replica
  State:     running
  Health:    healthy
  Port:      localhost:5433

==================================================
```

**Status**: ✓ PASS / ✗ FAIL

---

### Test 5: Comprehensive Replication Test

**Purpose**: Run built-in replication tests

```powershell
Test-PostgreSQLReplication
```

**Expected Result**:
```
==================================================
Testing PostgreSQL Replication
==================================================

Test 1: Container Status
✓ Primary container is running
✓ Replica container is running

Test 2: Replication Status
✓ Replication is active (streaming mode)

Test 3: Replication Lag
✓ Replication lag: 0 bytes, 0 seconds

Test 4: Write Test
✓ Successfully wrote test data to primary
✓ Successfully read test data from replica
✓ Replication is working correctly!

==================================================
Replication test complete!
==================================================
```

**Status**: ✓ PASS / ✗ FAIL

---

### Test 6: Replication Lag Metrics

**Purpose**: Verify replication lag monitoring

```powershell
$lag = Get-PostgreSQLReplicationLag
Write-Host "Lag Bytes: $($lag.LagBytes)"
Write-Host "Lag Seconds: $($lag.LagSeconds)"
Write-Host "State: $($lag.State)"
```

**Expected Result**:
- Lag Bytes: < 1000 (ideally 0)
- Lag Seconds: < 5 (ideally < 1)
- State: streaming

**Status**: ✓ PASS / ✗ FAIL

**Measured Lag**: _____ bytes, _____ seconds

---

### Test 7: Primary Database Connection

**Purpose**: Verify connectivity to primary database

```powershell
Invoke-PostgreSQLQuery -Query "SELECT version();" -Node Primary
```

**Expected Result**: Should display PostgreSQL version information (PostgreSQL 16.x)

**Test database access**:
```powershell
Invoke-PostgreSQLQuery -Query "\l" -Node Primary
```

Should show databases:
- postgres
- alarminsight
- alarminsight_hangfire

**Status**: ✓ PASS / ✗ FAIL

---

### Test 8: Replica Database Connection

**Purpose**: Verify connectivity to replica (read-only)

```powershell
Invoke-PostgreSQLQuery -Query "SELECT pg_is_in_recovery();" -Node Replica
```

**Expected Result**: Should return `t` (true), indicating replica is in recovery mode

**Status**: ✓ PASS / ✗ FAIL

---

### Test 9: Replication Stream Verification

**Purpose**: Verify streaming replication details

```powershell
Invoke-PostgreSQLQuery -Query "SELECT * FROM pg_stat_replication;" -Node Primary
```

**Expected Result**: Should show one row with:
- application_name: walreceiver
- state: streaming
- client_addr: (replica IP)
- sync_state: async

**Status**: ✓ PASS / ✗ FAIL

---

### Test 10: Write-Read Consistency Test

**Purpose**: Verify data replication from primary to replica

```powershell
# Create test table and insert data on primary
Invoke-PostgreSQLQuery -Query "CREATE TABLE IF NOT EXISTS test_replication (id SERIAL, data TEXT, created TIMESTAMP DEFAULT NOW());" -Database alarminsight -Node Primary

Invoke-PostgreSQLQuery -Query "INSERT INTO test_replication (data) VALUES ('Test at $(Get-Date)');" -Database alarminsight -Node Primary

# Wait for replication
Start-Sleep -Seconds 2

# Read from replica
Invoke-PostgreSQLQuery -Query "SELECT * FROM test_replication ORDER BY id DESC LIMIT 1;" -Database alarminsight -Node Replica
```

**Expected Result**: Data written to primary should appear on replica within 2 seconds

**Status**: ✓ PASS / ✗ FAIL

---

### Test 11: Database Users and Permissions

**Purpose**: Verify database users are created correctly

```powershell
Invoke-PostgreSQLQuery -Query "SELECT rolname, rolreplication, rolsuper FROM pg_roles WHERE rolname IN ('postgres', 'replicator', 'hangfire_user', 'alarminsight_user');" -Node Primary
```

**Expected Result**:
```
       rolname       | rolreplication | rolsuper
---------------------+----------------+----------
 postgres            | t              | t
 replicator          | t              | f
 hangfire_user       | f              | f
 alarminsight_user   | f              | f
```

**Status**: ✓ PASS / ✗ FAIL

---

### Test 12: Database Extensions

**Purpose**: Verify required extensions are installed

```powershell
Invoke-PostgreSQLQuery -Query "SELECT * FROM pg_extension WHERE extname IN ('uuid-ossp', 'pgcrypto');" -Database alarminsight -Node Primary
```

**Expected Result**: Should show both extensions installed

**Status**: ✓ PASS / ✗ FAIL

---

### Test 13: Container Logs Check

**Purpose**: Verify no critical errors in logs

```powershell
Show-PostgreSQLClusterLogs -Tail 50
```

**Expected Result**:
- No ERROR or FATAL messages
- Should see "database system is ready to accept connections"
- Replica should show "streaming replication successfully connected"

**Status**: ✓ PASS / ✗ FAIL

---

### Test 14: Health Check Endpoints

**Purpose**: Verify Docker health checks are working

```bash
docker inspect bahyway-postgres-primary | grep -A 5 "Health"
docker inspect bahyway-postgres-replica | grep -A 5 "Health"
```

**Expected Result**: Both should show `"Status": "healthy"`

**Status**: ✓ PASS / ✗ FAIL

---

### Test 15: PgAdmin Access

**Purpose**: Verify PgAdmin web interface is accessible

1. Open browser to http://localhost:5050
2. Login with:
   - Email: admin@bahyway.com
   - Password: admin

3. Add Primary Server:
   - Host: postgres-primary
   - Port: 5432
   - Username: postgres
   - Password: postgres_admin_pass

4. Add Replica Server:
   - Host: postgres-replica
   - Port: 5432
   - Username: postgres
   - Password: postgres_admin_pass

**Expected Result**: Both servers connect successfully and databases are visible

**Status**: ✓ PASS / ✗ FAIL

---

### Test 16: Backup and Restore

**Purpose**: Verify backup functionality

```powershell
# Create backup directory
$backupPath = "./backups"
New-Item -ItemType Directory -Path $backupPath -Force

# Backup database
Backup-PostgreSQLCluster -BackupPath $backupPath -Database alarminsight

# Verify backup file created
Get-ChildItem $backupPath
```

**Expected Result**:
- Backup file created successfully
- File size > 0 bytes
- No error messages

**Status**: ✓ PASS / ✗ FAIL

---

### Test 17: Cluster Restart

**Purpose**: Verify cluster can restart gracefully

```powershell
Restart-PostgreSQLCluster -Wait
```

**Expected Result**:
- Cluster stops gracefully
- Cluster starts successfully
- Health checks pass
- Data persists after restart

**Verify data persistence**:
```powershell
Invoke-PostgreSQLQuery -Query "SELECT COUNT(*) FROM test_replication;" -Database alarminsight -Node Primary
```

**Status**: ✓ PASS / ✗ FAIL

---

### Test 18: Performance Under Load

**Purpose**: Test replication lag under write load

```powershell
# Insert multiple records rapidly
1..100 | ForEach-Object {
    Invoke-PostgreSQLQuery -Query "INSERT INTO test_replication (data) VALUES ('Load test $_');" -Database alarminsight -Node Primary
}

# Check replication lag
Start-Sleep -Seconds 1
$lag = Get-PostgreSQLReplicationLag
Write-Host "Lag under load: $($lag.LagBytes) bytes, $($lag.LagSeconds) seconds"

# Verify count on replica
Start-Sleep -Seconds 2
Invoke-PostgreSQLQuery -Query "SELECT COUNT(*) FROM test_replication;" -Database alarminsight -Node Replica
```

**Expected Result**:
- Lag remains < 5 seconds
- All data replicates to replica
- No errors in logs

**Status**: ✓ PASS / ✗ FAIL

**Measured Lag Under Load**: _____ bytes, _____ seconds

---

## Integration Tests with AlarmInsight API

### Test 19: Start AlarmInsight API

**Purpose**: Verify API integration with PostgreSQL cluster

```powershell
# From automation-module directory
./Start-AlarmInsightAPI.ps1
```

**Expected Result**:
- Cluster starts successfully
- API builds successfully
- API starts on http://localhost:5000
- No connection errors

**Status**: ✓ PASS / ✗ FAIL

---

### Test 20: API Health Endpoints

**Purpose**: Verify API can monitor PostgreSQL health

1. Open browser to http://localhost:5000/swagger
2. Test endpoints:
   - GET /api/postgresql/health
   - GET /api/postgresql/healthz
   - GET /api/postgresql/replication

**Expected Result**: All endpoints return healthy status

**Status**: ✓ PASS / ✗ FAIL

---

### Test 21: Hangfire Dashboard

**Purpose**: Verify Hangfire can connect to PostgreSQL

1. Open browser to http://localhost:5000/hangfire
2. Verify dashboard loads
3. Check for background jobs

**Expected Result**: Dashboard loads without errors, PostgreSQL health job visible

**Status**: ✓ PASS / ✗ FAIL

---

## Cleanup Tests

### Test 22: Stop Cluster (Preserve Data)

**Purpose**: Verify cluster stops gracefully

```powershell
Stop-PostgreSQLCluster
```

**Expected Result**:
- All containers stop gracefully
- No error messages
- Volumes preserved

**Verify volumes exist**:
```bash
docker volume ls | grep bahyway
```

**Status**: ✓ PASS / ✗ FAIL

---

### Test 23: Complete Cleanup

**Purpose**: Verify complete removal works

```powershell
Remove-PostgreSQLCluster -Force
```

**Expected Result**:
- All containers removed
- All volumes removed
- Network removed
- No orphaned resources

**Verify cleanup**:
```bash
docker ps -a | grep bahyway  # Should return nothing
docker volume ls | grep bahyway  # Should return nothing
docker network ls | grep bahyway  # Should return nothing
```

**Status**: ✓ PASS / ✗ FAIL

---

## Test Summary

| Test # | Test Name | Status | Notes |
|--------|-----------|--------|-------|
| 1 | Module Commands | ☐ | |
| 2 | Initialize Cluster | ☐ | |
| 3 | Start Cluster | ☐ | Time: __s |
| 4 | Cluster Status | ☐ | |
| 5 | Replication Test | ☐ | |
| 6 | Replication Lag | ☐ | Lag: __ bytes, __s |
| 7 | Primary Connection | ☐ | |
| 8 | Replica Connection | ☐ | |
| 9 | Replication Stream | ☐ | |
| 10 | Write-Read Consistency | ☐ | |
| 11 | Database Users | ☐ | |
| 12 | Extensions | ☐ | |
| 13 | Container Logs | ☐ | |
| 14 | Health Checks | ☐ | |
| 15 | PgAdmin Access | ☐ | |
| 16 | Backup/Restore | ☐ | |
| 17 | Cluster Restart | ☐ | |
| 18 | Performance Load | ☐ | Lag: __ bytes, __s |
| 19 | API Start | ☐ | |
| 20 | API Health | ☐ | |
| 21 | Hangfire Dashboard | ☐ | |
| 22 | Stop Cluster | ☐ | |
| 23 | Complete Cleanup | ☐ | |

**Overall Result**: _____ / 23 tests passed

---

## Troubleshooting

### Common Issues and Solutions

#### Issue: Containers won't start

**Symptoms**:
- Docker Compose fails to start
- Health checks never pass
- Timeout errors

**Solutions**:
```powershell
# Check Docker is running
docker ps

# View logs
Show-PostgreSQLClusterLogs

# Try clean start
Remove-PostgreSQLCluster -Force
Start-PostgreSQLCluster -Wait -Timeout 300
```

#### Issue: Replication not working

**Symptoms**:
- Replica not in recovery mode
- No streaming connection
- Data not replicating

**Solutions**:
```powershell
# Check replication status
Invoke-PostgreSQLQuery -Query "SELECT * FROM pg_stat_replication;" -Node Primary

# Check replica logs
Show-PostgreSQLClusterLogs -Service Replica -Tail 100

# Verify replicator user exists
Invoke-PostgreSQLQuery -Query "SELECT * FROM pg_roles WHERE rolname='replicator';" -Node Primary
```

#### Issue: High replication lag

**Symptoms**:
- Lag > 5 seconds
- Lag bytes increasing

**Solutions**:
```powershell
# Check system resources
docker stats

# Check network connectivity
docker exec bahyway-postgres-replica ping postgres-primary

# Increase Docker resources (Memory/CPU)
# Check docker-compose.yml for resource limits
```

#### Issue: Port already in use

**Symptoms**:
- Error binding to port 5432 or 5433

**Solutions**:
```bash
# Find process using port
netstat -ano | findstr :5432  # Windows
lsof -i :5432                 # Linux/Mac

# Stop conflicting service or change port in docker-compose.yml
```

---

## Performance Benchmarks

Expected performance metrics:

| Metric | Target | Acceptable | Poor |
|--------|--------|------------|------|
| **Startup Time** | < 90s | < 180s | > 180s |
| **Replication Lag** | < 1s | < 5s | > 5s |
| **Lag Under Load** | < 2s | < 10s | > 10s |
| **Health Check Interval** | 10s | 15s | > 20s |
| **Write Throughput** | > 1000/s | > 500/s | < 500/s |

---

## Automated Testing Script

For automated testing, use this PowerShell script:

```powershell
# Save as: run-all-tests.ps1

$results = @()

function Run-Test {
    param($Name, $ScriptBlock)
    try {
        & $ScriptBlock
        $results += @{Name=$Name; Status="PASS"}
        Write-Host "✓ $Name - PASS" -ForegroundColor Green
    } catch {
        $results += @{Name=$Name; Status="FAIL"; Error=$_.Exception.Message}
        Write-Host "✗ $Name - FAIL: $_" -ForegroundColor Red
    }
}

Import-Module ./BahyWay.PostgreSQLReplication

Run-Test "Initialize Cluster" { Initialize-PostgreSQLCluster }
Run-Test "Start Cluster" { Start-PostgreSQLCluster -Wait }
Run-Test "Test Replication" { Test-PostgreSQLReplication }
Run-Test "Check Status" { Get-PostgreSQLClusterStatus }

# Print summary
$passed = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$total = $results.Count
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test Results: $passed / $total passed" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
```

---

## Continuous Integration Testing

For CI/CD pipelines, use this workflow:

```yaml
# Example GitHub Actions workflow
name: PostgreSQL Replication Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Install PowerShell
        run: |
          sudo apt-get update
          sudo apt-get install -y powershell

      - name: Start Docker Compose
        run: |
          cd infrastructure/postgresql-ha
          docker-compose up -d
          sleep 60

      - name: Run Tests
        shell: pwsh
        run: |
          cd infrastructure/postgresql-ha/automation-module
          Import-Module ./BahyWay.PostgreSQLReplication
          Test-PostgreSQLReplication

      - name: Cleanup
        if: always()
        run: |
          cd infrastructure/postgresql-ha
          docker-compose down -v
```

---

## Next Steps

After completing all tests:

1. **Document Results**: Fill in the test summary table
2. **Report Issues**: Create GitHub issues for any failed tests
3. **Performance Tuning**: If lag is high, tune PostgreSQL settings
4. **Production Readiness**: Review security settings before production use
5. **Monitoring Setup**: Configure production monitoring and alerting

---

## Additional Resources

- [PostgreSQL Replication Documentation](https://www.postgresql.org/docs/current/high-availability.html)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [PowerShell Module Guide](./automation-module/README.md)
- [Quick Start Guide](./automation-module/QUICKSTART.md)

---

**Document Version**: 1.0
**Last Updated**: 2025-11-24
**Author**: BahyWay Development Team
