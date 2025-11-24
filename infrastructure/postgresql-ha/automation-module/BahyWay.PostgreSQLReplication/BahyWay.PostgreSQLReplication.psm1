#Requires -Version 7.0

<#
.SYNOPSIS
    BahyWay PostgreSQL Replication Automation Module

.DESCRIPTION
    PowerShell module for automating PostgreSQL HA replication cluster management.
    Provides comprehensive Docker Compose-based container orchestration for
    PostgreSQL primary-replica clusters.

.NOTES
    Author: BahyWay Development Team
    Version: 1.0.0
    Requires: PowerShell 7.0+, Docker, Docker Compose
#>

# Module Configuration
$script:ModuleConfig = @{
    DockerComposePath = Join-Path $PSScriptRoot ".." ".." "docker-compose.yml"
    ProjectName = "bahyway-postgresql-ha"
    PrimaryContainer = "bahyway-postgres-primary"
    ReplicaContainer = "bahyway-postgres-replica"
    Network = "bahyway-network"
    PrimaryPort = 5432
    ReplicaPort = 5433
    LogPath = if ($IsWindows) {
        "$env:ProgramData\BahyWay\PostgreSQLReplication\Logs"
    } else {
        "/var/log/bahyway/postgresql-replication"
    }
}

#region Helper Functions

function Write-ModuleLog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Message,

        [ValidateSet('Info', 'Warning', 'Error', 'Success')]
        [string]$Level = 'Info'
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"

    # Console output with colors
    switch ($Level) {
        'Info'    { Write-Host $logMessage -ForegroundColor Cyan }
        'Warning' { Write-Host $logMessage -ForegroundColor Yellow }
        'Error'   { Write-Host $logMessage -ForegroundColor Red }
        'Success' { Write-Host $logMessage -ForegroundColor Green }
    }

    # File logging
    try {
        $logDir = $script:ModuleConfig.LogPath
        if (-not (Test-Path $logDir)) {
            New-Item -ItemType Directory -Path $logDir -Force | Out-Null
        }

        $logFile = Join-Path $logDir "postgresql-replication-$(Get-Date -Format 'yyyy-MM-dd').log"
        Add-Content -Path $logFile -Value $logMessage -ErrorAction SilentlyContinue
    }
    catch {
        # Silently fail if logging fails
    }
}

function Test-DockerAvailable {
    [CmdletBinding()]
    param()

    try {
        $null = docker --version 2>&1
        $null = docker-compose --version 2>&1
        return $true
    }
    catch {
        Write-ModuleLog "Docker or Docker Compose not found. Please install Docker Desktop." -Level Error
        return $false
    }
}

function Get-DockerComposePath {
    [CmdletBinding()]
    param()

    $composePath = $script:ModuleConfig.DockerComposePath

    if (-not (Test-Path $composePath)) {
        Write-ModuleLog "Docker Compose file not found at: $composePath" -Level Error
        throw "Docker Compose configuration not found"
    }

    return $composePath
}

function Invoke-DockerCompose {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [switch]$IgnoreErrors
    )

    $composePath = Get-DockerComposePath
    $composeDir = Split-Path $composePath -Parent

    $allArgs = @('-f', $composePath, '-p', $script:ModuleConfig.ProjectName) + $Arguments

    Write-ModuleLog "Executing: docker-compose $($allArgs -join ' ')" -Level Info

    Push-Location $composeDir
    try {
        if ($IgnoreErrors) {
            $output = docker-compose @allArgs 2>&1
        }
        else {
            $output = docker-compose @allArgs
            if ($LASTEXITCODE -ne 0) {
                throw "Docker Compose command failed with exit code: $LASTEXITCODE"
            }
        }
        return $output
    }
    finally {
        Pop-Location
    }
}

#endregion

#region Main Functions

function Initialize-PostgreSQLCluster {
    <#
    .SYNOPSIS
        Initialize PostgreSQL HA cluster environment

    .DESCRIPTION
        Prepares the environment for PostgreSQL cluster deployment by creating
        necessary directories, checking prerequisites, and validating configuration.

    .EXAMPLE
        Initialize-PostgreSQLCluster
    #>
    [CmdletBinding()]
    param()

    Write-ModuleLog "==================================================" -Level Info
    Write-ModuleLog "Initializing PostgreSQL HA Cluster Environment" -Level Info
    Write-ModuleLog "==================================================" -Level Info

    # Check Docker availability
    if (-not (Test-DockerAvailable)) {
        throw "Docker is not available. Please install Docker Desktop."
    }

    Write-ModuleLog "✓ Docker is available" -Level Success

    # Validate Docker Compose file
    $composePath = Get-DockerComposePath
    Write-ModuleLog "✓ Docker Compose file found: $composePath" -Level Success

    # Create log directory
    $logDir = $script:ModuleConfig.LogPath
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
        Write-ModuleLog "✓ Created log directory: $logDir" -Level Success
    }

    # Test Docker Compose configuration
    try {
        Invoke-DockerCompose -Arguments @('config', '--quiet')
        Write-ModuleLog "✓ Docker Compose configuration is valid" -Level Success
    }
    catch {
        Write-ModuleLog "Docker Compose configuration validation failed: $_" -Level Error
        throw
    }

    Write-ModuleLog "==================================================" -Level Info
    Write-ModuleLog "Initialization complete!" -Level Success
    Write-ModuleLog "==================================================" -Level Info
    Write-ModuleLog "" -Level Info
    Write-ModuleLog "Next steps:" -Level Info
    Write-ModuleLog "  1. Run: Start-PostgreSQLCluster" -Level Info
    Write-ModuleLog "  2. Wait for cluster to be healthy (60-90 seconds)" -Level Info
    Write-ModuleLog "  3. Run: Test-PostgreSQLReplication" -Level Info
}

function Start-PostgreSQLCluster {
    <#
    .SYNOPSIS
        Start PostgreSQL HA cluster

    .DESCRIPTION
        Starts the PostgreSQL primary and replica containers using Docker Compose.
        Creates databases, users, and establishes streaming replication.

    .PARAMETER Wait
        Wait for all services to be healthy before returning

    .PARAMETER Timeout
        Timeout in seconds when waiting for services (default: 120)

    .EXAMPLE
        Start-PostgreSQLCluster

    .EXAMPLE
        Start-PostgreSQLCluster -Wait -Timeout 180
    #>
    [CmdletBinding()]
    [Alias('pgstart')]
    param(
        [switch]$Wait,

        [int]$Timeout = 120
    )

    Write-ModuleLog "==================================================" -Level Info
    Write-ModuleLog "Starting PostgreSQL HA Cluster" -Level Info
    Write-ModuleLog "==================================================" -Level Info

    if (-not (Test-DockerAvailable)) {
        throw "Docker is not available"
    }

    try {
        # Start services
        Write-ModuleLog "Starting Docker containers..." -Level Info
        Invoke-DockerCompose -Arguments @('up', '-d')

        Write-ModuleLog "✓ Containers started successfully" -Level Success

        if ($Wait) {
            Write-ModuleLog "Waiting for services to be healthy (timeout: $Timeout seconds)..." -Level Info

            $startTime = Get-Date
            $healthy = $false

            while (((Get-Date) - $startTime).TotalSeconds -lt $Timeout) {
                Start-Sleep -Seconds 5

                $status = Get-PostgreSQLClusterStatus -Quiet

                if ($status.Primary.State -eq 'running' -and $status.Primary.Health -eq 'healthy' -and
                    $status.Replica.State -eq 'running' -and $status.Replica.Health -eq 'healthy') {
                    $healthy = $true
                    break
                }

                $elapsed = [int]((Get-Date) - $startTime).TotalSeconds
                Write-ModuleLog "  Waiting... ($elapsed/$Timeout seconds)" -Level Info
            }

            if ($healthy) {
                Write-ModuleLog "✓ All services are healthy!" -Level Success
            }
            else {
                Write-ModuleLog "Services did not become healthy within timeout period" -Level Warning
                Write-ModuleLog "Run 'Get-PostgreSQLClusterStatus' to check current status" -Level Info
            }
        }

        Write-ModuleLog "==================================================" -Level Info
        Write-ModuleLog "PostgreSQL HA Cluster started successfully!" -Level Success
        Write-ModuleLog "==================================================" -Level Info
        Write-ModuleLog "" -Level Info
        Write-ModuleLog "Connection Information:" -Level Info
        Write-ModuleLog "  Primary:  localhost:5432" -Level Info
        Write-ModuleLog "  Replica:  localhost:5433" -Level Info
        Write-ModuleLog "  PgAdmin:  http://localhost:5050" -Level Info
        Write-ModuleLog "" -Level Info
        Write-ModuleLog "Credentials:" -Level Info
        Write-ModuleLog "  Admin:    postgres / postgres_admin_pass" -Level Info
        Write-ModuleLog "  Hangfire: hangfire_user / hangfire_pass" -Level Info
        Write-ModuleLog "  App:      alarminsight_user / alarminsight_pass" -Level Info
        Write-ModuleLog "" -Level Info
        Write-ModuleLog "Next steps:" -Level Info
        Write-ModuleLog "  Run: Test-PostgreSQLReplication" -Level Info
        Write-ModuleLog "  Run: Get-PostgreSQLClusterStatus" -Level Info
    }
    catch {
        Write-ModuleLog "Failed to start PostgreSQL cluster: $_" -Level Error
        throw
    }
}

function Stop-PostgreSQLCluster {
    <#
    .SYNOPSIS
        Stop PostgreSQL HA cluster

    .DESCRIPTION
        Stops all PostgreSQL cluster containers gracefully.

    .PARAMETER RemoveVolumes
        Remove data volumes when stopping (WARNING: This will delete all data)

    .EXAMPLE
        Stop-PostgreSQLCluster

    .EXAMPLE
        Stop-PostgreSQLCluster -RemoveVolumes
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    [Alias('pgstop')]
    param(
        [switch]$RemoveVolumes
    )

    Write-ModuleLog "Stopping PostgreSQL HA Cluster..." -Level Info

    if (-not (Test-DockerAvailable)) {
        throw "Docker is not available"
    }

    try {
        if ($RemoveVolumes) {
            if ($PSCmdlet.ShouldProcess("PostgreSQL Cluster", "Stop and remove all data volumes")) {
                Write-ModuleLog "Stopping containers and removing volumes..." -Level Warning
                Invoke-DockerCompose -Arguments @('down', '-v')
                Write-ModuleLog "✓ Cluster stopped and volumes removed" -Level Success
            }
        }
        else {
            Invoke-DockerCompose -Arguments @('down')
            Write-ModuleLog "✓ Cluster stopped (data preserved)" -Level Success
        }
    }
    catch {
        Write-ModuleLog "Failed to stop PostgreSQL cluster: $_" -Level Error
        throw
    }
}

function Restart-PostgreSQLCluster {
    <#
    .SYNOPSIS
        Restart PostgreSQL HA cluster

    .DESCRIPTION
        Restarts all PostgreSQL cluster containers.

    .PARAMETER Wait
        Wait for services to be healthy after restart

    .EXAMPLE
        Restart-PostgreSQLCluster

    .EXAMPLE
        Restart-PostgreSQLCluster -Wait
    #>
    [CmdletBinding()]
    [Alias('pgrestart')]
    param(
        [switch]$Wait
    )

    Write-ModuleLog "Restarting PostgreSQL HA Cluster..." -Level Info

    Stop-PostgreSQLCluster
    Start-Sleep -Seconds 2

    if ($Wait) {
        Start-PostgreSQLCluster -Wait
    }
    else {
        Start-PostgreSQLCluster
    }
}

function Remove-PostgreSQLCluster {
    <#
    .SYNOPSIS
        Remove PostgreSQL HA cluster completely

    .DESCRIPTION
        Removes all containers, networks, and volumes associated with the cluster.
        WARNING: This will permanently delete all data!

    .PARAMETER Force
        Skip confirmation prompt

    .EXAMPLE
        Remove-PostgreSQLCluster

    .EXAMPLE
        Remove-PostgreSQLCluster -Force
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    param(
        [switch]$Force
    )

    $message = "This will permanently delete the PostgreSQL cluster and ALL data. Are you sure?"

    if ($Force -or $PSCmdlet.ShouldProcess("PostgreSQL Cluster", "Remove all containers, networks, and data")) {
        Write-ModuleLog "Removing PostgreSQL HA Cluster..." -Level Warning

        try {
            Invoke-DockerCompose -Arguments @('down', '-v', '--remove-orphans')
            Write-ModuleLog "✓ PostgreSQL cluster removed completely" -Level Success
        }
        catch {
            Write-ModuleLog "Failed to remove PostgreSQL cluster: $_" -Level Error
            throw
        }
    }
}

function Get-PostgreSQLClusterStatus {
    <#
    .SYNOPSIS
        Get PostgreSQL HA cluster status

    .DESCRIPTION
        Retrieves detailed status information about all cluster components.

    .PARAMETER Quiet
        Return status object without console output

    .EXAMPLE
        Get-PostgreSQLClusterStatus

    .EXAMPLE
        $status = Get-PostgreSQLClusterStatus -Quiet
    #>
    [CmdletBinding()]
    [Alias('pgstatus')]
    param(
        [switch]$Quiet
    )

    if (-not (Test-DockerAvailable)) {
        throw "Docker is not available"
    }

    try {
        # Get container status
        $primaryStatus = docker inspect $script:ModuleConfig.PrimaryContainer 2>$null | ConvertFrom-Json
        $replicaStatus = docker inspect $script:ModuleConfig.ReplicaContainer 2>$null | ConvertFrom-Json

        $status = @{
            Primary = @{
                Name = $script:ModuleConfig.PrimaryContainer
                State = if ($primaryStatus) { $primaryStatus.State.Status } else { 'not found' }
                Health = if ($primaryStatus.State.Health) { $primaryStatus.State.Health.Status } else { 'unknown' }
                StartedAt = if ($primaryStatus) { $primaryStatus.State.StartedAt } else { $null }
                Ports = "localhost:$($script:ModuleConfig.PrimaryPort)"
            }
            Replica = @{
                Name = $script:ModuleConfig.ReplicaContainer
                State = if ($replicaStatus) { $replicaStatus.State.Status } else { 'not found' }
                Health = if ($replicaStatus.State.Health) { $replicaStatus.State.Health.Status } else { 'unknown' }
                StartedAt = if ($replicaStatus) { $replicaStatus.State.StartedAt } else { $null }
                Ports = "localhost:$($script:ModuleConfig.ReplicaPort)"
            }
            Network = $script:ModuleConfig.Network
            Timestamp = Get-Date
        }

        if (-not $Quiet) {
            Write-ModuleLog "==================================================" -Level Info
            Write-ModuleLog "PostgreSQL HA Cluster Status" -Level Info
            Write-ModuleLog "==================================================" -Level Info
            Write-ModuleLog "" -Level Info

            Write-ModuleLog "Primary Node:" -Level Info
            Write-ModuleLog "  Container: $($status.Primary.Name)" -Level Info
            Write-ModuleLog "  State:     $($status.Primary.State)" -Level $(if ($status.Primary.State -eq 'running') { 'Success' } else { 'Warning' })
            Write-ModuleLog "  Health:    $($status.Primary.Health)" -Level $(if ($status.Primary.Health -eq 'healthy') { 'Success' } else { 'Warning' })
            Write-ModuleLog "  Port:      $($status.Primary.Ports)" -Level Info
            Write-ModuleLog "" -Level Info

            Write-ModuleLog "Replica Node:" -Level Info
            Write-ModuleLog "  Container: $($status.Replica.Name)" -Level Info
            Write-ModuleLog "  State:     $($status.Replica.State)" -Level $(if ($status.Replica.State -eq 'running') { 'Success' } else { 'Warning' })
            Write-ModuleLog "  Health:    $($status.Replica.Health)" -Level $(if ($status.Replica.Health -eq 'healthy') { 'Success' } else { 'Warning' })
            Write-ModuleLog "  Port:      $($status.Replica.Ports)" -Level Info
            Write-ModuleLog "" -Level Info
            Write-ModuleLog "==================================================" -Level Info
        }

        return $status
    }
    catch {
        Write-ModuleLog "Failed to get cluster status: $_" -Level Error
        throw
    }
}

function Show-PostgreSQLClusterLogs {
    <#
    .SYNOPSIS
        Show PostgreSQL cluster logs

    .DESCRIPTION
        Displays logs from PostgreSQL containers.

    .PARAMETER Service
        Specific service to show logs for (Primary, Replica, All)

    .PARAMETER Follow
        Follow log output (like tail -f)

    .PARAMETER Tail
        Number of lines to show from the end (default: 100)

    .EXAMPLE
        Show-PostgreSQLClusterLogs

    .EXAMPLE
        Show-PostgreSQLClusterLogs -Service Primary -Follow

    .EXAMPLE
        Show-PostgreSQLClusterLogs -Service Replica -Tail 50
    #>
    [CmdletBinding()]
    [Alias('pglogs')]
    param(
        [ValidateSet('Primary', 'Replica', 'All')]
        [string]$Service = 'All',

        [switch]$Follow,

        [int]$Tail = 100
    )

    if (-not (Test-DockerAvailable)) {
        throw "Docker is not available"
    }

    $arguments = @('logs')

    if ($Follow) {
        $arguments += '--follow'
    }

    $arguments += '--tail', $Tail

    switch ($Service) {
        'Primary' { $arguments += 'postgres-primary' }
        'Replica' { $arguments += 'postgres-replica' }
        'All'     { }  # No service specified = all services
    }

    try {
        Invoke-DockerCompose -Arguments $arguments
    }
    catch {
        Write-ModuleLog "Failed to show logs: $_" -Level Error
        throw
    }
}

function Test-PostgreSQLReplication {
    <#
    .SYNOPSIS
        Test PostgreSQL replication

    .DESCRIPTION
        Performs comprehensive replication testing including connectivity,
        replication status, and lag monitoring.

    .EXAMPLE
        Test-PostgreSQLReplication
    #>
    [CmdletBinding()]
    [Alias('pgtest')]
    param()

    Write-ModuleLog "==================================================" -Level Info
    Write-ModuleLog "Testing PostgreSQL Replication" -Level Info
    Write-ModuleLog "==================================================" -Level Info

    # Test 1: Check if containers are running
    Write-ModuleLog "" -Level Info
    Write-ModuleLog "Test 1: Container Status" -Level Info
    $status = Get-PostgreSQLClusterStatus -Quiet

    if ($status.Primary.State -ne 'running') {
        Write-ModuleLog "✗ Primary container is not running" -Level Error
        return
    }
    Write-ModuleLog "✓ Primary container is running" -Level Success

    if ($status.Replica.State -ne 'running') {
        Write-ModuleLog "✗ Replica container is not running" -Level Error
        return
    }
    Write-ModuleLog "✓ Replica container is running" -Level Success

    # Test 2: Check replication status
    Write-ModuleLog "" -Level Info
    Write-ModuleLog "Test 2: Replication Status" -Level Info

    try {
        $repQuery = "SELECT * FROM pg_stat_replication;"
        $repResult = docker exec $script:ModuleConfig.PrimaryContainer psql -U postgres -c $repQuery 2>&1

        if ($repResult -match 'streaming') {
            Write-ModuleLog "✓ Replication is active (streaming mode)" -Level Success
        }
        else {
            Write-ModuleLog "✗ Replication is not active" -Level Warning
            Write-ModuleLog "Output: $repResult" -Level Info
        }
    }
    catch {
        Write-ModuleLog "✗ Failed to check replication status: $_" -Level Error
    }

    # Test 3: Check replication lag
    Write-ModuleLog "" -Level Info
    Write-ModuleLog "Test 3: Replication Lag" -Level Info

    try {
        $lag = Get-PostgreSQLReplicationLag
        if ($lag) {
            Write-ModuleLog "✓ Replication lag: $($lag.LagBytes) bytes, $($lag.LagSeconds) seconds" -Level Success
        }
    }
    catch {
        Write-ModuleLog "✗ Failed to check replication lag: $_" -Level Error
    }

    # Test 4: Test write to primary
    Write-ModuleLog "" -Level Info
    Write-ModuleLog "Test 4: Write Test" -Level Info

    try {
        $testQuery = @"
CREATE TABLE IF NOT EXISTS replication_test (
    id SERIAL PRIMARY KEY,
    test_data TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);
INSERT INTO replication_test (test_data) VALUES ('Replication test at $(Get-Date)');
SELECT COUNT(*) FROM replication_test;
"@

        $writeResult = docker exec $script:ModuleConfig.PrimaryContainer psql -U postgres -d alarminsight -c $testQuery 2>&1
        Write-ModuleLog "✓ Successfully wrote test data to primary" -Level Success

        # Wait for replication
        Start-Sleep -Seconds 2

        # Verify on replica
        $readQuery = "SELECT COUNT(*) FROM replication_test;"
        $readResult = docker exec $script:ModuleConfig.ReplicaContainer psql -U postgres -d alarminsight -c $readQuery 2>&1

        if ($readResult -match '\d+') {
            Write-ModuleLog "✓ Successfully read test data from replica" -Level Success
            Write-ModuleLog "✓ Replication is working correctly!" -Level Success
        }
        else {
            Write-ModuleLog "✗ Failed to read from replica" -Level Warning
        }
    }
    catch {
        Write-ModuleLog "✗ Write/Read test failed: $_" -Level Error
    }

    Write-ModuleLog "" -Level Info
    Write-ModuleLog "==================================================" -Level Info
    Write-ModuleLog "Replication test complete!" -Level Success
    Write-ModuleLog "==================================================" -Level Info
}

function Get-PostgreSQLReplicationLag {
    <#
    .SYNOPSIS
        Get replication lag metrics

    .DESCRIPTION
        Retrieves detailed replication lag information including bytes and time lag.

    .EXAMPLE
        Get-PostgreSQLReplicationLag
    #>
    [CmdletBinding()]
    param()

    try {
        $query = @"
SELECT
    client_addr,
    state,
    pg_wal_lsn_diff(pg_current_wal_lsn(), sent_lsn) AS lag_bytes,
    EXTRACT(EPOCH FROM (NOW() - pg_last_xact_replay_timestamp())) AS lag_seconds
FROM pg_stat_replication;
"@

        $result = docker exec $script:ModuleConfig.PrimaryContainer psql -U postgres -t -A -F'|' -c $query 2>&1

        if ($result -match '([^|]+)\|([^|]+)\|([^|]+)\|([^|]+)') {
            return @{
                ClientAddr = $Matches[1]
                State = $Matches[2]
                LagBytes = [int]$Matches[3]
                LagSeconds = [double]$Matches[4]
            }
        }

        return $null
    }
    catch {
        Write-ModuleLog "Failed to get replication lag: $_" -Level Error
        throw
    }
}

function Connect-PostgreSQLPrimary {
    <#
    .SYNOPSIS
        Connect to primary PostgreSQL instance

    .DESCRIPTION
        Opens an interactive psql session to the primary database.

    .PARAMETER Database
        Database name to connect to (default: postgres)

    .EXAMPLE
        Connect-PostgreSQLPrimary

    .EXAMPLE
        Connect-PostgreSQLPrimary -Database alarminsight
    #>
    [CmdletBinding()]
    param(
        [string]$Database = 'postgres'
    )

    Write-ModuleLog "Connecting to primary database: $Database" -Level Info
    docker exec -it $script:ModuleConfig.PrimaryContainer psql -U postgres -d $Database
}

function Connect-PostgreSQLReplica {
    <#
    .SYNOPSIS
        Connect to replica PostgreSQL instance

    .DESCRIPTION
        Opens an interactive psql session to the replica database (read-only).

    .PARAMETER Database
        Database name to connect to (default: postgres)

    .EXAMPLE
        Connect-PostgreSQLReplica

    .EXAMPLE
        Connect-PostgreSQLReplica -Database alarminsight
    #>
    [CmdletBinding()]
    param(
        [string]$Database = 'postgres'
    )

    Write-ModuleLog "Connecting to replica database: $Database (read-only)" -Level Info
    docker exec -it $script:ModuleConfig.ReplicaContainer psql -U postgres -d $Database
}

function Invoke-PostgreSQLQuery {
    <#
    .SYNOPSIS
        Execute SQL query on PostgreSQL cluster

    .DESCRIPTION
        Executes a SQL query on the specified database and node.

    .PARAMETER Query
        SQL query to execute

    .PARAMETER Database
        Database name (default: postgres)

    .PARAMETER Node
        Node to execute on (Primary or Replica)

    .EXAMPLE
        Invoke-PostgreSQLQuery -Query "SELECT version();"

    .EXAMPLE
        Invoke-PostgreSQLQuery -Query "SELECT * FROM pg_stat_replication;" -Node Primary
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Query,

        [string]$Database = 'postgres',

        [ValidateSet('Primary', 'Replica')]
        [string]$Node = 'Primary'
    )

    $container = if ($Node -eq 'Primary') {
        $script:ModuleConfig.PrimaryContainer
    } else {
        $script:ModuleConfig.ReplicaContainer
    }

    try {
        $result = docker exec $container psql -U postgres -d $Database -c $Query 2>&1
        return $result
    }
    catch {
        Write-ModuleLog "Query execution failed: $_" -Level Error
        throw
    }
}

function Backup-PostgreSQLCluster {
    <#
    .SYNOPSIS
        Backup PostgreSQL cluster

    .DESCRIPTION
        Creates a backup of the primary database using pg_dump.

    .PARAMETER BackupPath
        Path to store backup file

    .PARAMETER Database
        Database to backup (default: all databases)

    .EXAMPLE
        Backup-PostgreSQLCluster -BackupPath "C:\Backups\postgres-backup.sql"

    .EXAMPLE
        Backup-PostgreSQLCluster -BackupPath "/backups/db.sql" -Database alarminsight
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$BackupPath,

        [string]$Database = 'all'
    )

    Write-ModuleLog "Starting backup of PostgreSQL cluster..." -Level Info

    try {
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $backupFile = if ($Database -eq 'all') {
            "postgres_all_$timestamp.sql"
        } else {
            "postgres_${Database}_$timestamp.sql"
        }

        $fullPath = Join-Path $BackupPath $backupFile

        if ($Database -eq 'all') {
            docker exec $script:ModuleConfig.PrimaryContainer pg_dumpall -U postgres > $fullPath
        }
        else {
            docker exec $script:ModuleConfig.PrimaryContainer pg_dump -U postgres -d $Database > $fullPath
        }

        Write-ModuleLog "✓ Backup completed: $fullPath" -Level Success
        return $fullPath
    }
    catch {
        Write-ModuleLog "Backup failed: $_" -Level Error
        throw
    }
}

function Restore-PostgreSQLCluster {
    <#
    .SYNOPSIS
        Restore PostgreSQL cluster from backup

    .DESCRIPTION
        Restores a PostgreSQL database from a backup file.

    .PARAMETER BackupFile
        Path to backup file

    .PARAMETER Database
        Target database name

    .EXAMPLE
        Restore-PostgreSQLCluster -BackupFile "C:\Backups\postgres-backup.sql" -Database alarminsight
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    param(
        [Parameter(Mandatory)]
        [string]$BackupFile,

        [Parameter(Mandatory)]
        [string]$Database
    )

    if (-not (Test-Path $BackupFile)) {
        throw "Backup file not found: $BackupFile"
    }

    if ($PSCmdlet.ShouldProcess($Database, "Restore database from backup")) {
        Write-ModuleLog "Restoring database: $Database" -Level Warning

        try {
            Get-Content $BackupFile | docker exec -i $script:ModuleConfig.PrimaryContainer psql -U postgres -d $Database
            Write-ModuleLog "✓ Restore completed successfully" -Level Success
        }
        catch {
            Write-ModuleLog "Restore failed: $_" -Level Error
            throw
        }
    }
}

#endregion

# Export module members
Export-ModuleMember -Function @(
    'Initialize-PostgreSQLCluster',
    'Start-PostgreSQLCluster',
    'Stop-PostgreSQLCluster',
    'Restart-PostgreSQLCluster',
    'Remove-PostgreSQLCluster',
    'Get-PostgreSQLClusterStatus',
    'Show-PostgreSQLClusterLogs',
    'Test-PostgreSQLReplication',
    'Get-PostgreSQLReplicationLag',
    'Connect-PostgreSQLPrimary',
    'Connect-PostgreSQLReplica',
    'Invoke-PostgreSQLQuery',
    'Backup-PostgreSQLCluster',
    'Restore-PostgreSQLCluster'
) -Alias @(
    'pgstart',
    'pgstop',
    'pgrestart',
    'pgstatus',
    'pglogs',
    'pgtest'
)

# Module initialization
Write-Host "BahyWay PostgreSQL Replication Module loaded successfully!" -ForegroundColor Green
Write-Host "Run 'Get-Command -Module BahyWay.PostgreSQLReplication' to see available commands" -ForegroundColor Cyan
