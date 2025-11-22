#Requires -Version 7.0

<#
.SYNOPSIS
    BahyWay PostgreSQL High Availability Management Module

.DESCRIPTION
    Enterprise-grade PowerShell module for managing PostgreSQL HA clusters with Docker,
    HAProxy, and Barman. Includes comprehensive health checks, monitoring, and alarm detection.

.NOTES
    Author: Bahaa Fadam - BahyWay Solutions
    Version: 1.0.0
    Date: 2025-11-22
#>

#region Module Variables

# Default configuration
$script:ModuleConfig = @{
    LogPath = if ($IsLinux -or $IsMacOS) {
        "/var/log/bahyway/postgresql-ha"
    } else {
        "$env:ProgramData\BahyWay\PostgreSQLHA\Logs"
    }
    AlarmLogPath = if ($IsLinux -or $IsMacOS) {
        "/var/log/bahyway/postgresql-ha/alarms"
    } else {
        "$env:ProgramData\BahyWay\PostgreSQLHA\Alarms"
    }
    ConfigPath = if ($IsLinux -or $IsMacOS) {
        "/etc/bahyway/postgresql-ha"
    } else {
        "$env:ProgramData\BahyWay\PostgreSQLHA\Config"
    }
    DefaultDockerComposeFile = "docker-compose-complete.yml"
    PrimaryContainerName = "bahyway-postgres-primary"
    ReplicaContainerName = "bahyway-postgres-replica"
    HAProxyContainerName = "bahyway-haproxy"
    BarmanContainerName = "bahyway-barman"
    NetworkName = "bahyway-network"
    MinimumDiskSpaceGB = 50
    ReplicationLagThresholdSeconds = 5
    AlarmRetentionDays = 30
}

# Global alarm registry
$script:HealthAlarms = @{}

#endregion

#region Helper Functions

function Write-ModuleLog {
    <#
    .SYNOPSIS
        Writes a log entry to the module log file
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('INFO', 'WARNING', 'ERROR', 'SUCCESS', 'DEBUG')]
        [string]$Level,
        
        [Parameter(Mandatory)]
        [string]$Message,
        
        [string]$Component = 'General',
        
        [System.Exception]$Exception
    )
    
    try {
        # Ensure log directory exists
        $logDir = $script:ModuleConfig.LogPath
        if (-not (Test-Path $logDir)) {
            New-Item -Path $logDir -ItemType Directory -Force | Out-Null
        }
        
        # Create log entry
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $logFile = Join-Path $logDir "postgresql-ha_$(Get-Date -Format 'yyyyMMdd').log"
        
        $logEntry = "[$timestamp] [$Level] [$Component] $Message"
        
        if ($Exception) {
            $logEntry += "`n    Exception: $($Exception.Message)"
            $logEntry += "`n    StackTrace: $($Exception.StackTrace)"
        }
        
        # Write to file
        Add-Content -Path $logFile -Value $logEntry -Encoding UTF8
        
        # Also write to console with color
        $color = switch ($Level) {
            'INFO' { 'White' }
            'SUCCESS' { 'Green' }
            'WARNING' { 'Yellow' }
            'ERROR' { 'Red' }
            'DEBUG' { 'Gray' }
        }
        Write-Host $logEntry -ForegroundColor $color
        
    } catch {
        Write-Warning "Failed to write to log: $_"
    }
}

function Write-AlarmLog {
    <#
    .SYNOPSIS
        Writes an alarm entry to the alarm log and registry
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$AlarmType,
        
        [Parameter(Mandatory)]
        [ValidateSet('Critical', 'High', 'Medium', 'Low', 'Info')]
        [string]$Severity,
        
        [Parameter(Mandatory)]
        [string]$Message,
        
        [string]$Component,
        
        [hashtable]$Details
    )
    
    try {
        # Ensure alarm directory exists
        $alarmDir = $script:ModuleConfig.AlarmLogPath
        if (-not (Test-Path $alarmDir)) {
            New-Item -Path $alarmDir -ItemType Directory -Force | Out-Null
        }
        
        # Create alarm object
        $alarm = [PSCustomObject]@{
            Timestamp = Get-Date
            AlarmType = $AlarmType
            Severity = $Severity
            Component = $Component
            Message = $Message
            Details = $Details
            Acknowledged = $false
            AcknowledgedAt = $null
            AcknowledgedBy = $null
        }
        
        # Add to global registry
        $alarmKey = "$AlarmType-$Component-$(Get-Date -Format 'yyyyMMddHHmmss')"
        $script:HealthAlarms[$alarmKey] = $alarm
        
        # Write to alarm log file
        $alarmFile = Join-Path $alarmDir "alarms_$(Get-Date -Format 'yyyyMMdd').json"
        
        $alarmJson = $alarm | ConvertTo-Json -Depth 10
        Add-Content -Path $alarmFile -Value $alarmJson -Encoding UTF8
        
        # Log to main log
        Write-ModuleLog -Level 'WARNING' -Component $Component -Message "ALARM: [$Severity] $AlarmType - $Message"
        
        return $alarm
        
    } catch {
        Write-ModuleLog -Level 'ERROR' -Component 'AlarmSystem' -Message "Failed to write alarm" -Exception $_
    }
}

function Test-CommandExists {
    <#
    .SYNOPSIS
        Tests if a command exists in the current session
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Command
    )
    
    return $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
}

function Invoke-DockerCommand {
    <#
    .SYNOPSIS
        Executes a Docker command with error handling
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Arguments,
        
        [switch]$IgnoreError
    )
    
    try {
        $result = Invoke-Expression "docker $Arguments 2>&1"
        
        if ($LASTEXITCODE -ne 0 -and -not $IgnoreError) {
            throw "Docker command failed: $result"
        }
        
        return $result
    } catch {
        if (-not $IgnoreError) {
            throw
        }
        return $null
    }
}

#endregion

#region Docker Environment Tests

function Test-DockerEnvironment {
    <#
    .SYNOPSIS
        Comprehensive test of Docker environment availability and health
    
    .DESCRIPTION
        Checks if Docker is installed, running, and accessible. Works on Windows WSL2 and Linux.
        Generates alarms if Docker is not available or unhealthy.
    
    .EXAMPLE
        Test-DockerEnvironment
        
    .EXAMPLE
        $result = Test-DockerEnvironment -Verbose
        if ($result.IsHealthy) { "Docker is ready" }
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param()
    
    begin {
        Write-ModuleLog -Level 'INFO' -Component 'Docker' -Message "Starting Docker environment test"
    }
    
    process {
        $result = [PSCustomObject]@{
            IsHealthy = $false
            DockerInstalled = $false
            DockerRunning = $false
            DockerVersion = $null
            Platform = $null
            IsWSL = $false
            Issues = @()
            Details = @{}
        }
        
        try {
            # Detect platform
            $result.Platform = if ($IsLinux) { "Linux" } 
                              elseif ($IsMacOS) { "MacOS" }
                              else { "Windows" }
            
            # Check if running in WSL
            if ($IsLinux -and (Test-Path "/proc/version")) {
                $versionContent = Get-Content "/proc/version" -Raw
                $result.IsWSL = $versionContent -match "Microsoft|WSL"
            }
            
            Write-ModuleLog -Level 'INFO' -Component 'Docker' -Message "Platform: $($result.Platform)$(if ($result.IsWSL) { ' (WSL2)' })"
            
            # Test 1: Check if Docker command exists
            if (-not (Test-CommandExists -Command 'docker')) {
                $result.Issues += "Docker command not found in PATH"
                Write-AlarmLog -AlarmType 'DockerNotInstalled' -Severity 'Critical' `
                    -Component 'Docker' -Message "Docker is not installed or not in PATH" `
                    -Details @{ Platform = $result.Platform; IsWSL = $result.IsWSL }
                
                Write-ModuleLog -Level 'ERROR' -Component 'Docker' -Message "Docker not found"
                return $result
            }
            
            $result.DockerInstalled = $true
            Write-ModuleLog -Level 'SUCCESS' -Component 'Docker' -Message "Docker command found"
            
            # Test 2: Get Docker version
            try {
                $versionOutput = docker version --format '{{.Server.Version}}' 2>&1
                if ($LASTEXITCODE -eq 0) {
                    $result.DockerVersion = $versionOutput
                    $result.Details.Version = $versionOutput
                    Write-ModuleLog -Level 'INFO' -Component 'Docker' -Message "Docker version: $versionOutput"
                }
            } catch {
                $result.Issues += "Cannot retrieve Docker version"
            }
            
            # Test 3: Check if Docker daemon is running
            try {
                $psOutput = docker ps 2>&1
                if ($LASTEXITCODE -eq 0) {
                    $result.DockerRunning = $true
                    Write-ModuleLog -Level 'SUCCESS' -Component 'Docker' -Message "Docker daemon is running"
                } else {
                    $result.Issues += "Docker daemon is not running or not accessible"
                    Write-AlarmLog -AlarmType 'DockerNotRunning' -Severity 'Critical' `
                        -Component 'Docker' -Message "Docker daemon is not running" `
                        -Details @{ Error = $psOutput; Platform = $result.Platform }
                    
                    Write-ModuleLog -Level 'ERROR' -Component 'Docker' -Message "Docker daemon not running"
                    return $result
                }
            } catch {
                $result.Issues += "Error checking Docker daemon: $_"
                Write-ModuleLog -Level 'ERROR' -Component 'Docker' -Message "Error checking daemon" -Exception $_
                return $result
            }
            
            # Test 4: Check Docker info
            try {
                $infoOutput = docker info --format '{{json .}}' 2>&1 | ConvertFrom-Json
                $result.Details.ContainersRunning = $infoOutput.ContainersRunning
                $result.Details.Images = $infoOutput.Images
                $result.Details.ServerVersion = $infoOutput.ServerVersion
                
                Write-ModuleLog -Level 'INFO' -Component 'Docker' -Message "Containers running: $($infoOutput.ContainersRunning)"
            } catch {
                Write-ModuleLog -Level 'WARNING' -Component 'Docker' -Message "Could not retrieve Docker info"
            }
            
            # If all tests passed
            if ($result.DockerInstalled -and $result.DockerRunning -and $result.Issues.Count -eq 0) {
                $result.IsHealthy = $true
                Write-ModuleLog -Level 'SUCCESS' -Component 'Docker' -Message "Docker environment is healthy"
            }
            
        } catch {
            $result.Issues += "Unexpected error: $_"
            Write-ModuleLog -Level 'ERROR' -Component 'Docker' -Message "Unexpected error in Docker test" -Exception $_
        } finally {
            Write-ModuleLog -Level 'INFO' -Component 'Docker' -Message "Docker environment test completed"
        }
        
        return $result
    }
}

#endregion

#region PostgreSQL Health Tests

function Test-PostgreSQLPrimary {
    <#
    .SYNOPSIS
        Tests the health of the PostgreSQL primary node
    
    .DESCRIPTION
        Comprehensive health check of the primary PostgreSQL container including:
        - Container existence and running state
        - PostgreSQL process status
        - Database connectivity
        - Replication configuration
        - Resource usage
    
    .PARAMETER ContainerName
        Name of the primary container (default: bahyway-postgres-primary)
    
    .EXAMPLE
        Test-PostgreSQLPrimary
        
    .EXAMPLE
        Test-PostgreSQLPrimary -ContainerName "my-postgres-primary" -Verbose
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [string]$ContainerName = $script:ModuleConfig.PrimaryContainerName
    )
    
    begin {
        Write-ModuleLog -Level 'INFO' -Component 'PostgreSQL-Primary' -Message "Starting primary node health check"
    }
    
    process {
        $result = [PSCustomObject]@{
            IsHealthy = $false
            ContainerExists = $false
            ContainerRunning = $false
            PostgreSQLResponding = $false
            IsInRecovery = $null
            ReplicationConfigured = $false
            ActiveConnections = 0
            DatabaseSize = $null
            Issues = @()
            Details = @{}
        }
        
        try {
            # Test 1: Check if container exists
            try {
                $containerInfo = docker ps -a --filter "name=^${ContainerName}$" --format '{{json .}}' 2>&1
                
                if ($LASTEXITCODE -eq 0 -and $containerInfo) {
                    $container = $containerInfo | ConvertFrom-Json
                    $result.ContainerExists = $true
                    $result.Details.ContainerId = $container.ID
                    $result.Details.Status = $container.Status
                    $result.Details.State = $container.State
                    
                    Write-ModuleLog -Level 'SUCCESS' -Component 'PostgreSQL-Primary' `
                        -Message "Container found: $($container.Status)"
                } else {
                    $result.Issues += "Primary container does not exist"
                    Write-AlarmLog -AlarmType 'PrimaryContainerMissing' -Severity 'Critical' `
                        -Component 'PostgreSQL-Primary' -Message "Primary container '$ContainerName' not found"
                    
                    Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Primary' -Message "Container not found"
                    return $result
                }
            } catch {
                $result.Issues += "Error checking container: $_"
                Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Primary' -Message "Error checking container" -Exception $_
                return $result
            }
            
            # Test 2: Check if container is running
            if ($result.Details.State -eq 'running') {
                $result.ContainerRunning = $true
                Write-ModuleLog -Level 'SUCCESS' -Component 'PostgreSQL-Primary' -Message "Container is running"
            } else {
                $result.Issues += "Container is not running (State: $($result.Details.State))"
                Write-AlarmLog -AlarmType 'PrimaryContainerNotRunning' -Severity 'Critical' `
                    -Component 'PostgreSQL-Primary' -Message "Primary container is not running" `
                    -Details @{ State = $result.Details.State; ContainerName = $ContainerName }
                
                Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Primary' -Message "Container not running"
                return $result
            }
            
            # Test 3: Check PostgreSQL process
            try {
                $pgReady = docker exec $ContainerName pg_isready -U postgres 2>&1
                if ($LASTEXITCODE -eq 0 -and $pgReady -match "accepting connections") {
                    $result.PostgreSQLResponding = $true
                    Write-ModuleLog -Level 'SUCCESS' -Component 'PostgreSQL-Primary' -Message "PostgreSQL is accepting connections"
                } else {
                    $result.Issues += "PostgreSQL not accepting connections"
                    Write-AlarmLog -AlarmType 'PrimaryDatabaseNotResponding' -Severity 'Critical' `
                        -Component 'PostgreSQL-Primary' -Message "PostgreSQL is not accepting connections"
                    
                    Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Primary' -Message "Database not accepting connections"
                }
            } catch {
                $result.Issues += "Error checking PostgreSQL: $_"
                Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Primary' -Message "Error checking PostgreSQL" -Exception $_
            }
            
            # Test 4: Check if in recovery mode (should be FALSE for primary)
            if ($result.PostgreSQLResponding) {
                try {
                    $recoveryStatus = docker exec $ContainerName psql -U postgres -t -A -c "SELECT pg_is_in_recovery();" 2>&1
                    $result.IsInRecovery = ($recoveryStatus -match "t")
                    
                    if ($result.IsInRecovery) {
                        $result.Issues += "Primary is in recovery mode (should not be)"
                        Write-AlarmLog -AlarmType 'PrimaryInRecoveryMode' -Severity 'Critical' `
                            -Component 'PostgreSQL-Primary' -Message "Primary node is incorrectly in recovery mode"
                        
                        Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Primary' -Message "Primary in recovery mode"
                    } else {
                        Write-ModuleLog -Level 'SUCCESS' -Component 'PostgreSQL-Primary' -Message "Primary is not in recovery mode (correct)"
                    }
                } catch {
                    Write-ModuleLog -Level 'WARNING' -Component 'PostgreSQL-Primary' -Message "Could not check recovery status"
                }
                
                # Test 5: Check replication configuration
                try {
                    $replCount = docker exec $ContainerName psql -U postgres -t -A -c "SELECT COUNT(*) FROM pg_stat_replication;" 2>&1
                    $result.Details.ReplicaCount = [int]$replCount
                    
                    if ($replCount -gt 0) {
                        $result.ReplicationConfigured = $true
                        Write-ModuleLog -Level 'SUCCESS' -Component 'PostgreSQL-Primary' `
                            -Message "Replication active with $replCount replica(s)"
                    } else {
                        $result.Issues += "No replicas connected"
                        Write-AlarmLog -AlarmType 'NoReplicasConnected' -Severity 'High' `
                            -Component 'PostgreSQL-Primary' -Message "No replica nodes are connected to primary"
                        
                        Write-ModuleLog -Level 'WARNING' -Component 'PostgreSQL-Primary' -Message "No replicas connected"
                    }
                } catch {
                    Write-ModuleLog -Level 'WARNING' -Component 'PostgreSQL-Primary' -Message "Could not check replication status"
                }
                
                # Test 6: Get active connections
                try {
                    $connCount = docker exec $ContainerName psql -U postgres -t -A -c "SELECT count(*) FROM pg_stat_activity WHERE state = 'active';" 2>&1
                    $result.ActiveConnections = [int]$connCount
                    $result.Details.ActiveConnections = $result.ActiveConnections
                    Write-ModuleLog -Level 'INFO' -Component 'PostgreSQL-Primary' -Message "Active connections: $connCount"
                } catch {
                    Write-ModuleLog -Level 'WARNING' -Component 'PostgreSQL-Primary' -Message "Could not get connection count"
                }
                
                # Test 7: Get database size
                try {
                    $dbSize = docker exec $ContainerName psql -U postgres -t -A -c "SELECT pg_size_pretty(pg_database_size('alarminsight'));" 2>&1
                    $result.DatabaseSize = $dbSize.Trim()
                    $result.Details.DatabaseSize = $result.DatabaseSize
                    Write-ModuleLog -Level 'INFO' -Component 'PostgreSQL-Primary' -Message "Database size: $($result.DatabaseSize)"
                } catch {
                    Write-ModuleLog -Level 'WARNING' -Component 'PostgreSQL-Primary' -Message "Could not get database size"
                }
            }
            
            # Determine overall health
            if ($result.ContainerRunning -and 
                $result.PostgreSQLResponding -and 
                -not $result.IsInRecovery -and
                $result.Issues.Count -eq 0) {
                $result.IsHealthy = $true
                Write-ModuleLog -Level 'SUCCESS' -Component 'PostgreSQL-Primary' -Message "Primary node is healthy"
            } else {
                Write-ModuleLog -Level 'WARNING' -Component 'PostgreSQL-Primary' -Message "Primary node has issues"
            }
            
        } catch {
            $result.Issues += "Unexpected error: $_"
            Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Primary' -Message "Unexpected error" -Exception $_
        } finally {
            Write-ModuleLog -Level 'INFO' -Component 'PostgreSQL-Primary' -Message "Primary health check completed"
        }
        
        return $result
    }
}

function Test-PostgreSQLReplica {
    <#
    .SYNOPSIS
        Tests the health of the PostgreSQL replica node
    
    .DESCRIPTION
        Comprehensive health check of the replica PostgreSQL container including:
        - Container existence and running state
        - PostgreSQL process status
        - Recovery mode verification
        - Replication lag monitoring
        - Database synchronization status
    
    .PARAMETER ContainerName
        Name of the replica container (default: bahyway-postgres-replica)
    
    .EXAMPLE
        Test-PostgreSQLReplica
        
    .EXAMPLE
        Test-PostgreSQLReplica -ContainerName "my-postgres-replica" -Verbose
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [string]$ContainerName = $script:ModuleConfig.ReplicaContainerName
    )
    
    begin {
        Write-ModuleLog -Level 'INFO' -Component 'PostgreSQL-Replica' -Message "Starting replica node health check"
    }
    
    process {
        $result = [PSCustomObject]@{
            IsHealthy = $false
            ContainerExists = $false
            ContainerRunning = $false
            PostgreSQLResponding = $false
            IsInRecovery = $null
            ReplicationActive = $false
            ReplicationLag = $null
            Issues = @()
            Details = @{}
        }
        
        try {
            # Test 1: Check if container exists
            try {
                $containerInfo = docker ps -a --filter "name=^${ContainerName}$" --format '{{json .}}' 2>&1
                
                if ($LASTEXITCODE -eq 0 -and $containerInfo) {
                    $container = $containerInfo | ConvertFrom-Json
                    $result.ContainerExists = $true
                    $result.Details.ContainerId = $container.ID
                    $result.Details.Status = $container.Status
                    $result.Details.State = $container.State
                    
                    Write-ModuleLog -Level 'SUCCESS' -Component 'PostgreSQL-Replica' `
                        -Message "Container found: $($container.Status)"
                } else {
                    $result.Issues += "Replica container does not exist"
                    Write-AlarmLog -AlarmType 'ReplicaContainerMissing' -Severity 'High' `
                        -Component 'PostgreSQL-Replica' -Message "Replica container '$ContainerName' not found"
                    
                    Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Replica' -Message "Container not found"
                    return $result
                }
            } catch {
                $result.Issues += "Error checking container: $_"
                Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Replica' -Message "Error checking container" -Exception $_
                return $result
            }
            
            # Test 2: Check if container is running
            if ($result.Details.State -eq 'running') {
                $result.ContainerRunning = $true
                Write-ModuleLog -Level 'SUCCESS' -Component 'PostgreSQL-Replica' -Message "Container is running"
            } else {
                $result.Issues += "Container is not running (State: $($result.Details.State))"
                Write-AlarmLog -AlarmType 'ReplicaContainerNotRunning' -Severity 'High' `
                    -Component 'PostgreSQL-Replica' -Message "Replica container is not running" `
                    -Details @{ State = $result.Details.State; ContainerName = $ContainerName }
                
                Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Replica' -Message "Container not running"
                return $result
            }
            
            # Test 3: Check PostgreSQL process
            try {
                $pgReady = docker exec $ContainerName pg_isready -U postgres 2>&1
                if ($LASTEXITCODE -eq 0 -and $pgReady -match "accepting connections") {
                    $result.PostgreSQLResponding = $true
                    Write-ModuleLog -Level 'SUCCESS' -Component 'PostgreSQL-Replica' -Message "PostgreSQL is accepting connections"
                } else {
                    $result.Issues += "PostgreSQL not accepting connections"
                    Write-AlarmLog -AlarmType 'ReplicaDatabaseNotResponding' -Severity 'High' `
                        -Component 'PostgreSQL-Replica' -Message "PostgreSQL replica is not accepting connections"
                    
                    Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Replica' -Message "Database not accepting connections"
                }
            } catch {
                $result.Issues += "Error checking PostgreSQL: $_"
                Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Replica' -Message "Error checking PostgreSQL" -Exception $_
            }
            
            # Test 4: Check if in recovery mode (should be TRUE for replica)
            if ($result.PostgreSQLResponding) {
                try {
                    $recoveryStatus = docker exec $ContainerName psql -U postgres -t -A -c "SELECT pg_is_in_recovery();" 2>&1
                    $result.IsInRecovery = ($recoveryStatus -match "t")
                    
                    if ($result.IsInRecovery) {
                        Write-ModuleLog -Level 'SUCCESS' -Component 'PostgreSQL-Replica' -Message "Replica is in recovery mode (correct)"
                    } else {
                        $result.Issues += "Replica is NOT in recovery mode (should be)"
                        Write-AlarmLog -AlarmType 'ReplicaNotInRecoveryMode' -Severity 'Critical' `
                            -Component 'PostgreSQL-Replica' -Message "Replica node is not in recovery mode"
                        
                        Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Replica' -Message "Replica not in recovery mode"
                    }
                } catch {
                    Write-ModuleLog -Level 'WARNING' -Component 'PostgreSQL-Replica' -Message "Could not check recovery status"
                }
                
                # Test 5: Check replication lag
                try {
                    $lagQuery = "SELECT EXTRACT(EPOCH FROM (now() - pg_last_xact_replay_timestamp()))::int;"
                    $lagSeconds = docker exec $ContainerName psql -U postgres -t -A -c $lagQuery 2>&1
                    
                    if ($lagSeconds -match '^\d+$') {
                        $result.ReplicationLag = [int]$lagSeconds
                        $result.Details.ReplicationLagSeconds = $result.ReplicationLag
                        
                        if ($result.ReplicationLag -gt $script:ModuleConfig.ReplicationLagThresholdSeconds) {
                            $result.Issues += "Replication lag is high: $($result.ReplicationLag) seconds"
                            Write-AlarmLog -AlarmType 'HighReplicationLag' -Severity 'Medium' `
                                -Component 'PostgreSQL-Replica' `
                                -Message "Replication lag is $($result.ReplicationLag) seconds (threshold: $($script:ModuleConfig.ReplicationLagThresholdSeconds))" `
                                -Details @{ LagSeconds = $result.ReplicationLag }
                            
                            Write-ModuleLog -Level 'WARNING' -Component 'PostgreSQL-Replica' `
                                -Message "High replication lag: $($result.ReplicationLag)s"
                        } else {
                            $result.ReplicationActive = $true
                            Write-ModuleLog -Level 'SUCCESS' -Component 'PostgreSQL-Replica' `
                                -Message "Replication lag is acceptable: $($result.ReplicationLag)s"
                        }
                    }
                } catch {
                    Write-ModuleLog -Level 'WARNING' -Component 'PostgreSQL-Replica' -Message "Could not check replication lag"
                }
            }
            
            # Determine overall health
            if ($result.ContainerRunning -and 
                $result.PostgreSQLResponding -and 
                $result.IsInRecovery -and
                $result.Issues.Count -eq 0) {
                $result.IsHealthy = $true
                Write-ModuleLog -Level 'SUCCESS' -Component 'PostgreSQL-Replica' -Message "Replica node is healthy"
            } else {
                Write-ModuleLog -Level 'WARNING' -Component 'PostgreSQL-Replica' -Message "Replica node has issues"
            }
            
        } catch {
            $result.Issues += "Unexpected error: $_"
            Write-ModuleLog -Level 'ERROR' -Component 'PostgreSQL-Replica' -Message "Unexpected error" -Exception $_
        } finally {
            Write-ModuleLog -Level 'INFO' -Component 'PostgreSQL-Replica' -Message "Replica health check completed"
        }
        
        return $result
    }
}

function Test-PostgreSQLReplication {
    <#
    .SYNOPSIS
        Tests the replication status between primary and replica
    
    .DESCRIPTION
        Verifies that streaming replication is working correctly by checking:
        - Primary replication connections
        - Replication lag metrics
        - WAL position synchronization
        - Replication slots
    
    .EXAMPLE
        Test-PostgreSQLReplication
        
    .EXAMPLE
        $status = Test-PostgreSQLReplication -Verbose
        if ($status.IsHealthy) { "Replication OK" }
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [string]$PrimaryContainer = $script:ModuleConfig.PrimaryContainerName,
        [string]$ReplicaContainer = $script:ModuleConfig.ReplicaContainerName
    )
    
    begin {
        Write-ModuleLog -Level 'INFO' -Component 'Replication' -Message "Starting replication status check"
    }
    
    process {
        $result = [PSCustomObject]@{
            IsHealthy = $false
            ReplicationActive = $false
            ReplicaConnected = $false
            StreamingState = $null
            SyncState = $null
            WriteLag = $null
            FlushLag = $null
            ReplayLag = $null
            ReplicationSlots = @()
            Issues = @()
            Details = @{}
        }
        
        try {
            # Check if primary is available
            $primaryTest = Test-PostgreSQLPrimary -ContainerName $PrimaryContainer
            if (-not $primaryTest.IsHealthy) {
                $result.Issues += "Primary node is not healthy"
                Write-ModuleLog -Level 'ERROR' -Component 'Replication' -Message "Cannot check replication: primary unhealthy"
                return $result
            }
            
            # Get replication status from primary
            try {
                $replQuery = @"
SELECT 
    client_addr,
    application_name,
    state,
    sync_state,
    COALESCE(write_lag, '0'::interval)::text as write_lag,
    COALESCE(flush_lag, '0'::interval)::text as flush_lag,
    COALESCE(replay_lag, '0'::interval)::text as replay_lag
FROM pg_stat_replication;
"@
                
                $replStatus = docker exec $PrimaryContainer psql -U postgres -t -A -F '|' -c $replQuery 2>&1
                
                if ($LASTEXITCODE -eq 0 -and $replStatus -and $replStatus -ne "") {
                    $fields = $replStatus.Split('|')
                    
                    $result.ReplicaConnected = $true
                    $result.Details.ClientAddress = $fields[0]
                    $result.Details.ApplicationName = $fields[1]
                    $result.StreamingState = $fields[2]
                    $result.SyncState = $fields[3]
                    $result.WriteLag = $fields[4]
                    $result.FlushLag = $fields[5]
                    $result.ReplayLag = $fields[6]
                    
                    Write-ModuleLog -Level 'SUCCESS' -Component 'Replication' `
                        -Message "Replica connected: $($result.StreamingState) ($($result.SyncState))"
                    
                    if ($result.StreamingState -eq 'streaming') {
                        $result.ReplicationActive = $true
                    } else {
                        $result.Issues += "Replication state is not 'streaming': $($result.StreamingState)"
                        Write-AlarmLog -AlarmType 'ReplicationNotStreaming' -Severity 'High' `
                            -Component 'Replication' `
                            -Message "Replication is not in streaming state" `
                            -Details @{ State = $result.StreamingState }
                    }
                } else {
                    $result.Issues += "No replication connections found"
                    Write-AlarmLog -AlarmType 'NoReplicationConnection' -Severity 'Critical' `
                        -Component 'Replication' -Message "No replica is connected to primary"
                    
                    Write-ModuleLog -Level 'ERROR' -Component 'Replication' -Message "No replication connections"
                }
            } catch {
                $result.Issues += "Error querying replication status: $_"
                Write-ModuleLog -Level 'ERROR' -Component 'Replication' -Message "Error querying replication" -Exception $_
            }
            
            # Check replication slots
            try {
                $slotsQuery = "SELECT slot_name, slot_type, active FROM pg_replication_slots;"
                $slots = docker exec $PrimaryContainer psql -U postgres -t -A -c $slotsQuery 2>&1
                
                if ($slots) {
                    $result.ReplicationSlots = @($slots -split "`n" | Where-Object { $_ })
                    Write-ModuleLog -Level 'INFO' -Component 'Replication' `
                        -Message "Replication slots: $($result.ReplicationSlots.Count)"
                }
            } catch {
                Write-ModuleLog -Level 'WARNING' -Component 'Replication' -Message "Could not check replication slots"
            }
            
            # Determine overall health
            if ($result.ReplicationActive -and $result.Issues.Count -eq 0) {
                $result.IsHealthy = $true
                Write-ModuleLog -Level 'SUCCESS' -Component 'Replication' -Message "Replication is healthy"
            }
            
        } catch {
            $result.Issues += "Unexpected error: $_"
            Write-ModuleLog -Level 'ERROR' -Component 'Replication' -Message "Unexpected error" -Exception $_
        } finally {
            Write-ModuleLog -Level 'INFO' -Component 'Replication' -Message "Replication check completed"
        }
        
        return $result
    }
}

#endregion

#region Storage and Resource Tests

function Test-StorageSpace {
    <#
    .SYNOPSIS
        Tests available storage space for PostgreSQL data
    
    .DESCRIPTION
        Checks available disk space on the host and within containers,
        generates alarms if space is running low.
    
    .PARAMETER MinimumSpaceGB
        Minimum required space in GB (default: 50)
    
    .EXAMPLE
        Test-StorageSpace
        
    .EXAMPLE
        Test-StorageSpace -MinimumSpaceGB 100 -Verbose
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [int]$MinimumSpaceGB = $script:ModuleConfig.MinimumDiskSpaceGB
    )
    
    begin {
        Write-ModuleLog -Level 'INFO' -Component 'Storage' -Message "Starting storage space check"
    }
    
    process {
        $result = [PSCustomObject]@{
            IsHealthy = $false
            HostStorageGB = 0
            HostAvailableGB = 0
            HostUsedPercent = 0
            VolumeInfo = @()
            Issues = @()
            Details = @{}
        }
        
        try {
            # Check host storage
            if ($IsLinux -or $IsMacOS) {
                try {
                    $dfOutput = df -BG / | Select-Object -Skip 1
                    $fields = $dfOutput -split '\s+' 
                    $result.HostStorageGB = [int]($fields[1] -replace 'G','')
                    $result.HostAvailableGB = [int]($fields[3] -replace 'G','')
                    $result.HostUsedPercent = [int]($fields[4] -replace '%','')
                    
                    Write-ModuleLog -Level 'INFO' -Component 'Storage' `
                        -Message "Host storage: $($result.HostAvailableGB)GB available ($($result.HostUsedPercent)% used)"
                } catch {
                    Write-ModuleLog -Level 'WARNING' -Component 'Storage' -Message "Could not check host storage"
                }
            } else {
                # Windows
                try {
                    $drive = Get-PSDrive -Name C
                    $result.HostStorageGB = [math]::Round($drive.Used / 1GB + $drive.Free / 1GB)
                    $result.HostAvailableGB = [math]::Round($drive.Free / 1GB)
                    $result.HostUsedPercent = [math]::Round(($drive.Used / ($drive.Used + $drive.Free)) * 100)
                    
                    Write-ModuleLog -Level 'INFO' -Component 'Storage' `
                        -Message "Host storage: $($result.HostAvailableGB)GB available ($($result.HostUsedPercent)% used)"
                } catch {
                    Write-ModuleLog -Level 'WARNING' -Component 'Storage' -Message "Could not check host storage"
                }
            }
            
            # Check if we have minimum required space
            if ($result.HostAvailableGB -lt $MinimumSpaceGB) {
                $result.Issues += "Insufficient storage: $($result.HostAvailableGB)GB available (minimum: ${MinimumSpaceGB}GB)"
                Write-AlarmLog -AlarmType 'LowDiskSpace' -Severity 'High' `
                    -Component 'Storage' `
                    -Message "Available storage ($($result.HostAvailableGB)GB) is below minimum ($MinimumSpaceGB GB)" `
                    -Details @{ AvailableGB = $result.HostAvailableGB; MinimumGB = $MinimumSpaceGB }
                
                Write-ModuleLog -Level 'ERROR' -Component 'Storage' -Message "Insufficient storage space"
            } else {
                Write-ModuleLog -Level 'SUCCESS' -Component 'Storage' -Message "Storage space is adequate"
            }
            
            # Check Docker volumes
            try {
                $volumes = docker volume ls --filter "name=bahyway" --format '{{.Name}}' 2>&1
                if ($volumes) {
                    foreach ($vol in $volumes) {
                        try {
                            $volInspect = docker volume inspect $vol 2>&1 | ConvertFrom-Json
                            $result.VolumeInfo += [PSCustomObject]@{
                                Name = $vol
                                Mountpoint = $volInspect.Mountpoint
                                Driver = $volInspect.Driver
                            }
                        } catch {
                            Write-ModuleLog -Level 'WARNING' -Component 'Storage' -Message "Could not inspect volume: $vol"
                        }
                    }
                    Write-ModuleLog -Level 'INFO' -Component 'Storage' -Message "Found $($result.VolumeInfo.Count) volumes"
                }
            } catch {
                Write-ModuleLog -Level 'WARNING' -Component 'Storage' -Message "Could not list Docker volumes"
            }
            
            # Determine health
            if ($result.HostAvailableGB -ge $MinimumSpaceGB -and $result.Issues.Count -eq 0) {
                $result.IsHealthy = $true
                Write-ModuleLog -Level 'SUCCESS' -Component 'Storage' -Message "Storage is healthy"
            }
            
        } catch {
            $result.Issues += "Unexpected error: $_"
            Write-ModuleLog -Level 'ERROR' -Component 'Storage' -Message "Unexpected error" -Exception $_
        } finally {
            Write-ModuleLog -Level 'INFO' -Component 'Storage' -Message "Storage check completed"
        }
        
        return $result
    }
}

#endregion

#region Comprehensive Cluster Health

function Get-ClusterHealth {
    <#
    .SYNOPSIS
        Performs a comprehensive health check of the entire PostgreSQL HA cluster
    
    .DESCRIPTION
        Runs all health checks and returns a complete status report including:
        - Docker environment
        - Primary node
        - Replica node
        - Replication status
        - Storage space
        - Network connectivity
        - HAProxy (if configured)
        - Barman (if configured)
    
    .PARAMETER IncludeHAProxy
        Include HAProxy health check
    
    .PARAMETER IncludeBarman
        Include Barman backup health check
    
    .EXAMPLE
        Get-ClusterHealth
        
    .EXAMPLE
        Get-ClusterHealth -IncludeHAProxy -IncludeBarman -Verbose
        
    .EXAMPLE
        $health = Get-ClusterHealth
        if (-not $health.IsHealthy) {
            $health.AllIssues | ForEach-Object { Write-Warning $_ }
        }
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [switch]$IncludeHAProxy,
        [switch]$IncludeBarman
    )
    
    begin {
        Write-ModuleLog -Level 'INFO' -Component 'ClusterHealth' -Message "========== STARTING COMPREHENSIVE CLUSTER HEALTH CHECK =========="
    }
    
    process {
        $result = [PSCustomObject]@{
            IsHealthy = $false
            Timestamp = Get-Date
            Docker = $null
            Primary = $null
            Replica = $null
            Replication = $null
            Storage = $null
            HAProxy = $null
            Barman = $null
            AllIssues = @()
            Summary = @{}
        }
        
        try {
            # 1. Docker Environment
            Write-Host "`n[1/7] Checking Docker Environment..." -ForegroundColor Cyan
            $result.Docker = Test-DockerEnvironment
            if (-not $result.Docker.IsHealthy) {
                $result.AllIssues += $result.Docker.Issues
                Write-ModuleLog -Level 'ERROR' -Component 'ClusterHealth' -Message "Docker environment check failed"
                return $result  # Can't continue without Docker
            }
            
            # 2. Primary Node
            Write-Host "`n[2/7] Checking Primary Node..." -ForegroundColor Cyan
            $result.Primary = Test-PostgreSQLPrimary
            $result.AllIssues += $result.Primary.Issues
            
            # 3. Replica Node
            Write-Host "`n[3/7] Checking Replica Node..." -ForegroundColor Cyan
            $result.Replica = Test-PostgreSQLReplica
            $result.AllIssues += $result.Replica.Issues
            
            # 4. Replication Status
            Write-Host "`n[4/7] Checking Replication..." -ForegroundColor Cyan
            $result.Replication = Test-PostgreSQLReplication
            $result.AllIssues += $result.Replication.Issues
            
            # 5. Storage
            Write-Host "`n[5/7] Checking Storage Space..." -ForegroundColor Cyan
            $result.Storage = Test-StorageSpace
            $result.AllIssues += $result.Storage.Issues
            
            # 6. HAProxy (optional)
            if ($IncludeHAProxy) {
                Write-Host "`n[6/7] Checking HAProxy..." -ForegroundColor Cyan
                $result.HAProxy = Test-HAProxyHealth
                if ($result.HAProxy) {
                    $result.AllIssues += $result.HAProxy.Issues
                }
            } else {
                Write-Host "`n[6/7] Skipping HAProxy check" -ForegroundColor Gray
            }
            
            # 7. Barman (optional)
            if ($IncludeBarman) {
                Write-Host "`n[7/7] Checking Barman..." -ForegroundColor Cyan
                $result.Barman = Test-BarmanBackup
                if ($result.Barman) {
                    $result.AllIssues += $result.Barman.Issues
                }
            } else {
                Write-Host "`n[7/7] Skipping Barman check" -ForegroundColor Gray
            }
            
            # Generate summary
            $result.Summary = @{
                TotalIssues = $result.AllIssues.Count
                DockerHealthy = $result.Docker.IsHealthy
                PrimaryHealthy = $result.Primary.IsHealthy
                ReplicaHealthy = $result.Replica.IsHealthy
                ReplicationHealthy = $result.Replication.IsHealthy
                StorageHealthy = $result.Storage.IsHealthy
                ReplicationLagSeconds = $result.Replica.ReplicationLag
                PrimaryActiveConnections = $result.Primary.ActiveConnections
                StorageAvailableGB = $result.Storage.HostAvailableGB
            }
            
            # Determine overall health
            $criticalComponentsHealthy = (
                $result.Docker.IsHealthy -and
                $result.Primary.IsHealthy -and
                $result.Replica.IsHealthy -and
                $result.Replication.IsHealthy -and
                $result.Storage.IsHealthy
            )
            
            if ($criticalComponentsHealthy -and $result.AllIssues.Count -eq 0) {
                $result.IsHealthy = $true
                Write-ModuleLog -Level 'SUCCESS' -Component 'ClusterHealth' -Message "✅ CLUSTER IS HEALTHY"
            } else {
                Write-ModuleLog -Level 'WARNING' -Component 'ClusterHealth' -Message "⚠️  CLUSTER HAS ISSUES"
            }
            
        } catch {
            $result.AllIssues += "Unexpected error in cluster health check: $_"
            Write-ModuleLog -Level 'ERROR' -Component 'ClusterHealth' -Message "Unexpected error" -Exception $_
        } finally {
            Write-ModuleLog -Level 'INFO' -Component 'ClusterHealth' -Message "========== CLUSTER HEALTH CHECK COMPLETED =========="
        }
        
        return $result
    }
    
    end {
        # Display summary
        Write-Host "`n╔════════════════════════════════════════════════════╗" -ForegroundColor Cyan
        Write-Host   "║          CLUSTER HEALTH SUMMARY                    ║" -ForegroundColor Cyan
        Write-Host   "╚════════════════════════════════════════════════════╝" -ForegroundColor Cyan
        
        if ($result.IsHealthy) {
            Write-Host "`n✅ ALL SYSTEMS OPERATIONAL" -ForegroundColor Green
        } else {
            Write-Host "`n⚠️  ISSUES DETECTED: $($result.AllIssues.Count)" -ForegroundColor Yellow
            foreach ($issue in $result.AllIssues) {
                Write-Host "   • $issue" -ForegroundColor Yellow
            }
        }
        
        Write-Host "`n📊 Component Status:" -ForegroundColor Cyan
        Write-Host "   Docker:       $(if ($result.Docker.IsHealthy) { '✅' } else { '❌' })" -ForegroundColor White
        Write-Host "   Primary:      $(if ($result.Primary.IsHealthy) { '✅' } else { '❌' })" -ForegroundColor White
        Write-Host "   Replica:      $(if ($result.Replica.IsHealthy) { '✅' } else { '❌' })" -ForegroundColor White
        Write-Host "   Replication:  $(if ($result.Replication.IsHealthy) { '✅' } else { '❌' })" -ForegroundColor White
        Write-Host "   Storage:      $(if ($result.Storage.IsHealthy) { '✅' } else { '❌' })" -ForegroundColor White
        
        if ($result.Replication.IsHealthy) {
            Write-Host "`n📈 Metrics:" -ForegroundColor Cyan
            Write-Host "   Replication Lag:  $($result.Replica.ReplicationLag)s" -ForegroundColor White
            Write-Host "   Active Connections: $($result.Primary.ActiveConnections)" -ForegroundColor White
            Write-Host "   Available Storage: $($result.Storage.HostAvailableGB)GB" -ForegroundColor White
        }
        
        Write-Host ""
    }
}

#endregion

# Export module members
Export-ModuleMember -Function * -Alias *
