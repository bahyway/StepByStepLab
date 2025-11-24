#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Start AlarmInsight API with PostgreSQL HA Cluster

.DESCRIPTION
    This script automates the complete setup and startup of:
    1. PostgreSQL HA Cluster (Primary + Replica)
    2. AlarmInsight API with Hangfire background jobs
    3. Health monitoring and replication testing

.PARAMETER SkipClusterStart
    Skip starting PostgreSQL cluster (use if already running)

.PARAMETER SkipTest
    Skip replication testing

.PARAMETER Clean
    Clean start - remove existing cluster and data

.PARAMETER ApiPort
    Port for AlarmInsight API (default: 5000)

.EXAMPLE
    ./Start-AlarmInsightAPI.ps1

.EXAMPLE
    ./Start-AlarmInsightAPI.ps1 -Clean

.EXAMPLE
    ./Start-AlarmInsightAPI.ps1 -SkipClusterStart -ApiPort 5001
#>

[CmdletBinding()]
param(
    [switch]$SkipClusterStart,
    [switch]$SkipTest,
    [switch]$Clean,
    [int]$ApiPort = 5000
)

# Script configuration
$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

$script:Config = @{
    ModulePath = Join-Path $PSScriptRoot "BahyWay.PostgreSQLReplication"
    ApiProjectPath = Join-Path $PSScriptRoot ".." ".." ".." "src" "AlarmInsight.API"
    ClusterWaitTimeout = 120
    ReplicationTestDelay = 10
}

#region Helper Functions

function Write-Banner {
    param([string]$Message)

    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([string]$Message)
    Write-Host "➜ $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-ErrorMessage {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Test-Prerequisites {
    Write-Step "Checking prerequisites..."

    # Check PowerShell version
    if ($PSVersionTable.PSVersion.Major -lt 7) {
        Write-ErrorMessage "PowerShell 7.0 or higher is required"
        Write-Host "Current version: $($PSVersionTable.PSVersion)" -ForegroundColor Red
        Write-Host "Download from: https://github.com/PowerShell/PowerShell/releases" -ForegroundColor Yellow
        return $false
    }
    Write-Success "PowerShell version: $($PSVersionTable.PSVersion)"

    # Check Docker
    try {
        $dockerVersion = docker --version 2>&1
        Write-Success "Docker: $dockerVersion"
    }
    catch {
        Write-ErrorMessage "Docker not found. Please install Docker Desktop."
        return $false
    }

    # Check Docker Compose
    try {
        $composeVersion = docker-compose --version 2>&1
        Write-Success "Docker Compose: $composeVersion"
    }
    catch {
        Write-ErrorMessage "Docker Compose not found. Please install Docker Desktop."
        return $false
    }

    # Check .NET SDK
    try {
        $dotnetVersion = dotnet --version 2>&1
        Write-Success ".NET SDK: $dotnetVersion"
    }
    catch {
        Write-ErrorMessage ".NET SDK not found. Please install .NET 8.0 SDK."
        Write-Host "Download from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        return $false
    }

    # Check if API project exists
    if (-not (Test-Path $script:Config.ApiProjectPath)) {
        Write-ErrorMessage "AlarmInsight.API project not found at: $($script:Config.ApiProjectPath)"
        return $false
    }
    Write-Success "AlarmInsight.API project found"

    return $true
}

function Import-PostgreSQLModule {
    Write-Step "Loading PostgreSQL Replication module..."

    $modulePath = $script:Config.ModulePath

    if (-not (Test-Path $modulePath)) {
        Write-ErrorMessage "Module not found at: $modulePath"
        throw "PostgreSQL Replication module not found"
    }

    # Remove if already loaded
    if (Get-Module BahyWay.PostgreSQLReplication) {
        Remove-Module BahyWay.PostgreSQLReplication -Force
    }

    Import-Module $modulePath -Force
    Write-Success "PostgreSQL Replication module loaded"
}

function Start-Cluster {
    Write-Step "Starting PostgreSQL HA Cluster..."

    if ($Clean) {
        Write-Host "⚠️  Clean start requested - removing existing cluster..." -ForegroundColor Yellow
        Remove-PostgreSQLCluster -Force -ErrorAction SilentlyContinue
    }

    Initialize-PostgreSQLCluster
    Start-PostgreSQLCluster -Wait -Timeout $script:Config.ClusterWaitTimeout

    Write-Success "PostgreSQL HA Cluster is running"
}

function Test-Replication {
    Write-Step "Testing PostgreSQL replication..."

    Write-Host "Waiting $($script:Config.ReplicationTestDelay) seconds for replication to stabilize..." -ForegroundColor Gray
    Start-Sleep -Seconds $script:Config.ReplicationTestDelay

    Test-PostgreSQLReplication

    Write-Success "Replication test completed"
}

function Show-ConnectionInfo {
    Write-Banner "Connection Information"

    Write-Host "PostgreSQL Primary:" -ForegroundColor Cyan
    Write-Host "  Host:     localhost:5432" -ForegroundColor White
    Write-Host "  Admin:    postgres / postgres_admin_pass" -ForegroundColor White
    Write-Host ""

    Write-Host "PostgreSQL Replica:" -ForegroundColor Cyan
    Write-Host "  Host:     localhost:5433" -ForegroundColor White
    Write-Host "  Admin:    postgres / postgres_admin_pass" -ForegroundColor White
    Write-Host ""

    Write-Host "Databases:" -ForegroundColor Cyan
    Write-Host "  alarminsight_hangfire:" -ForegroundColor White
    Write-Host "    User:   hangfire_user / hangfire_pass" -ForegroundColor White
    Write-Host "  alarminsight:" -ForegroundColor White
    Write-Host "    User:   alarminsight_user / alarminsight_pass" -ForegroundColor White
    Write-Host ""

    Write-Host "PgAdmin:" -ForegroundColor Cyan
    Write-Host "  URL:      http://localhost:5050" -ForegroundColor White
    Write-Host "  Email:    admin@bahyway.com" -ForegroundColor White
    Write-Host "  Password: admin" -ForegroundColor White
    Write-Host ""

    Write-Host "AlarmInsight API:" -ForegroundColor Cyan
    Write-Host "  URL:      http://localhost:$ApiPort" -ForegroundColor White
    Write-Host "  Swagger:  http://localhost:$ApiPort/swagger" -ForegroundColor White
    Write-Host ""
}

function Start-Api {
    Write-Step "Starting AlarmInsight API..."

    Push-Location $script:Config.ApiProjectPath

    try {
        Write-Host ""
        Write-Host "Building and starting API on port $ApiPort..." -ForegroundColor Cyan
        Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
        Write-Host ""

        # Set environment variables
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $env:ASPNETCORE_URLS = "http://localhost:$ApiPort"

        # Build and run
        dotnet build
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed"
        }

        Write-Success "Build completed"
        Write-Host ""
        Write-Host "Starting API..." -ForegroundColor Cyan
        Write-Host ""

        dotnet run --no-build
    }
    catch {
        Write-ErrorMessage "Failed to start API: $_"
        throw
    }
    finally {
        Pop-Location
    }
}

#endregion

#region Main Execution

try {
    Write-Banner "BahyWay AlarmInsight API Startup Script"

    # Step 1: Check prerequisites
    Write-Banner "Step 1: Checking Prerequisites"
    if (-not (Test-Prerequisites)) {
        throw "Prerequisites check failed"
    }

    # Step 2: Load PostgreSQL module
    Write-Banner "Step 2: Loading PostgreSQL Module"
    Import-PostgreSQLModule

    # Step 3: Start PostgreSQL cluster
    if (-not $SkipClusterStart) {
        Write-Banner "Step 3: Starting PostgreSQL HA Cluster"
        Start-Cluster
    }
    else {
        Write-Banner "Step 3: PostgreSQL Cluster (Skipped)"
        Write-Host "⚠️  Cluster start skipped - assuming cluster is already running" -ForegroundColor Yellow
        Get-PostgreSQLClusterStatus
    }

    # Step 4: Test replication
    if (-not $SkipTest) {
        Write-Banner "Step 4: Testing Replication"
        Test-Replication
    }
    else {
        Write-Banner "Step 4: Replication Test (Skipped)"
        Write-Host "⚠️  Replication test skipped" -ForegroundColor Yellow
    }

    # Step 5: Show connection info
    Show-ConnectionInfo

    # Step 6: Start API
    Write-Banner "Step 5: Starting AlarmInsight API"
    Start-Api
}
catch {
    Write-Host ""
    Write-ErrorMessage "Startup failed: $_"
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Check Docker is running: docker ps" -ForegroundColor Gray
    Write-Host "  2. Check cluster status: Get-PostgreSQLClusterStatus" -ForegroundColor Gray
    Write-Host "  3. Check cluster logs: Show-PostgreSQLClusterLogs" -ForegroundColor Gray
    Write-Host "  4. Try clean start: ./Start-AlarmInsightAPI.ps1 -Clean" -ForegroundColor Gray
    Write-Host ""

    exit 1
}
finally {
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "Cleanup / Shutdown" -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "API stopped. PostgreSQL cluster is still running." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To stop the cluster, run:" -ForegroundColor Yellow
    Write-Host "  Stop-PostgreSQLCluster" -ForegroundColor White
    Write-Host ""
    Write-Host "To view cluster status:" -ForegroundColor Yellow
    Write-Host "  Get-PostgreSQLClusterStatus" -ForegroundColor White
    Write-Host ""
}

#endregion
