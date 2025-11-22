#Requires -Version 7.0

<#
.SYNOPSIS
    Installs the BahyWay PostgreSQL HA Management Module

.DESCRIPTION
    This script installs the BahyWay.PostgreSQLHA PowerShell module on Windows or Linux.
    It creates necessary directories, copies module files, and sets up logging.

.PARAMETER Scope
    Installation scope: CurrentUser or AllUsers (default: CurrentUser)

.PARAMETER Force
    Force reinstallation even if module already exists

.EXAMPLE
    .\Install-Module.ps1

.EXAMPLE
    .\Install-Module.ps1 -Scope AllUsers -Force
#>

[CmdletBinding()]
param(
    [ValidateSet('CurrentUser', 'AllUsers')]
    [string]$Scope = 'CurrentUser',
    
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# Module information
$moduleName = 'BahyWay.PostgreSQLHA'
$moduleVersion = '1.0.0'

Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  BahyWay PostgreSQL HA Module Installation            ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

try {
    # Determine installation path
    if ($Scope -eq 'AllUsers') {
        if ($IsLinux -or $IsMacOS) {
            $modulePath = "/usr/local/share/powershell/Modules/$moduleName"
            $logPath = "/var/log/bahyway/postgresql-ha"
            $alarmPath = "/var/log/bahyway/postgresql-ha/alarms"
            $configPath = "/etc/bahyway/postgresql-ha"
        } else {
            $modulePath = "$env:ProgramFiles\PowerShell\Modules\$moduleName"
            $logPath = "$env:ProgramData\BahyWay\PostgreSQLHA\Logs"
            $alarmPath = "$env:ProgramData\BahyWay\PostgreSQLHA\Alarms"
            $configPath = "$env:ProgramData\BahyWay\PostgreSQLHA\Config"
        }
    } else {
        if ($IsLinux -or $IsMacOS) {
            $modulePath = "$HOME/.local/share/powershell/Modules/$moduleName"
            $logPath = "$HOME/.local/share/bahyway/postgresql-ha/logs"
            $alarmPath = "$HOME/.local/share/bahyway/postgresql-ha/alarms"
            $configPath = "$HOME/.config/bahyway/postgresql-ha"
        } else {
            $modulePath = "$HOME\Documents\PowerShell\Modules\$moduleName"
            $logPath = "$env:LOCALAPPDATA\BahyWay\PostgreSQLHA\Logs"
            $alarmPath = "$env:LOCALAPPDATA\BahyWay\PostgreSQLHA\Alarms"
            $configPath = "$env:LOCALAPPDATA\BahyWay\PostgreSQLHA\Config"
        }
    }
    
    Write-Host "[1/6] Checking prerequisites..." -ForegroundColor Yellow
    
    # Check PowerShell version
    if ($PSVersionTable.PSVersion.Major -lt 7) {
        throw "PowerShell 7.0 or higher is required. Current version: $($PSVersionTable.PSVersion)"
    }
    Write-Host "   ✅ PowerShell version OK: $($PSVersionTable.PSVersion)" -ForegroundColor Green
    
    # Check if Docker is available
    if (Get-Command docker -ErrorAction SilentlyContinue) {
        Write-Host "   ✅ Docker found" -ForegroundColor Green
    } else {
        Write-Warning "   ⚠️  Docker not found - module will work but health checks will fail"
    }
    
    # Check if module already exists
    if ((Test-Path $modulePath) -and -not $Force) {
        $response = Read-Host "Module already exists at $modulePath. Reinstall? (Y/N)"
        if ($response -ne 'Y') {
            Write-Host "Installation cancelled." -ForegroundColor Yellow
            return
        }
    }
    
    Write-Host "`n[2/6] Creating directories..." -ForegroundColor Yellow
    
    # Create module directory
    if (Test-Path $modulePath) {
        Remove-Item -Path $modulePath -Recurse -Force
    }
    New-Item -Path $modulePath -ItemType Directory -Force | Out-Null
    Write-Host "   ✅ Module directory: $modulePath" -ForegroundColor Green
    
    # Create log directories
    @($logPath, $alarmPath, $configPath) | ForEach-Object {
        if (-not (Test-Path $_)) {
            New-Item -Path $_ -ItemType Directory -Force | Out-Null
            Write-Host "   ✅ Created: $_" -ForegroundColor Green
        }
    }
    
    Write-Host "`n[3/6] Copying module files..." -ForegroundColor Yellow
    
    # Get script directory
    $scriptPath = $PSScriptRoot
    if (-not $scriptPath) {
        $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
    }
    
    # Copy module manifest
    $manifestSource = Join-Path $scriptPath "BahyWay.PostgreSQLHA.psd1"
    if (Test-Path $manifestSource) {
        Copy-Item -Path $manifestSource -Destination $modulePath -Force
        Write-Host "   ✅ Copied module manifest" -ForegroundColor Green
    } else {
        throw "Module manifest not found: $manifestSource"
    }
    
    # Copy module file
    $moduleSource = Join-Path $scriptPath "BahyWay.PostgreSQLHA.psm1"
    if (Test-Path $moduleSource) {
        Copy-Item -Path $moduleSource -Destination $modulePath -Force
        Write-Host "   ✅ Copied module file" -ForegroundColor Green
    } else {
        throw "Module file not found: $moduleSource"
    }
    
    # Copy additional functions if they exist
    $additionalSource = Join-Path $scriptPath "HAProxyBarman.ps1"
    if (Test-Path $additionalSource) {
        Copy-Item -Path $additionalSource -Destination (Join-Path $modulePath "HAProxyBarman.ps1") -Force
        Write-Host "   ✅ Copied additional functions" -ForegroundColor Green
    }
    
    Write-Host "`n[4/6] Creating default configuration..." -ForegroundColor Yellow
    
    # Create default config
    $config = @{
        module = @{
            name = $moduleName
            version = $moduleVersion
            installedAt = (Get-Date).ToString('o')
        }
        paths = @{
            logPath = $logPath
            alarmLogPath = $alarmPath
            configPath = $configPath
        }
        docker = @{
            primaryContainerName = "bahyway-postgres-primary"
            replicaContainerName = "bahyway-postgres-replica"
            haproxyContainerName = "bahyway-haproxy"
            barmanContainerName = "bahyway-barman"
            networkName = "bahyway-network"
        }
        thresholds = @{
            minimumDiskSpaceGB = 50
            replicationLagThresholdSeconds = 5
            alarmRetentionDays = 30
        }
        monitoring = @{
            healthCheckIntervalMinutes = 5
            enableAutomaticAlerts = $true
            alertEmail = ""
            alertWebhook = ""
        }
    }
    
    $configFile = Join-Path $configPath "config.json"
    $config | ConvertTo-Json -Depth 10 | Set-Content -Path $configFile -Encoding UTF8
    Write-Host "   ✅ Created configuration: $configFile" -ForegroundColor Green
    
    Write-Host "`n[5/6] Importing module..." -ForegroundColor Yellow
    
    # Remove old module if loaded
    if (Get-Module $moduleName) {
        Remove-Module $moduleName -Force
    }
    
    # Import module
    Import-Module $modulePath -Force
    $importedModule = Get-Module $moduleName
    
    if ($importedModule) {
        Write-Host "   ✅ Module imported successfully" -ForegroundColor Green
        Write-Host "   Version: $($importedModule.Version)" -ForegroundColor Green
        Write-Host "   Commands: $($importedModule.ExportedCommands.Count)" -ForegroundColor Green
    } else {
        throw "Failed to import module"
    }
    
    Write-Host "`n[6/6] Verifying installation..." -ForegroundColor Yellow
    
    # Test basic functionality
    try {
        $dockerTest = Test-DockerEnvironment -ErrorAction Stop
        if ($dockerTest.DockerInstalled) {
            Write-Host "   ✅ Module is functional" -ForegroundColor Green
        } else {
            Write-Warning "   ⚠️  Module loaded but Docker not available"
        }
    } catch {
        Write-Warning "   ⚠️  Module loaded but test failed: $_"
    }
    
    # Success message
    Write-Host "`n╔════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  INSTALLATION COMPLETED SUCCESSFULLY!                  ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Green
    
    Write-Host "`n📦 Installation Summary:" -ForegroundColor Cyan
    Write-Host "   Module:  $moduleName v$moduleVersion" -ForegroundColor White
    Write-Host "   Path:    $modulePath" -ForegroundColor White
    Write-Host "   Logs:    $logPath" -ForegroundColor White
    Write-Host "   Alarms:  $alarmPath" -ForegroundColor White
    Write-Host "   Config:  $configPath" -ForegroundColor White
    Write-Host "   Scope:   $Scope" -ForegroundColor White
    
    Write-Host "`n🚀 Quick Start:" -ForegroundColor Cyan
    Write-Host "   1. Import-Module $moduleName" -ForegroundColor White
    Write-Host "   2. Get-ClusterHealth" -ForegroundColor White
    Write-Host "   3. Get-Command -Module $moduleName  # See all commands" -ForegroundColor White
    
    Write-Host "`n📚 Documentation:" -ForegroundColor Cyan
    Write-Host "   Get-Help Get-ClusterHealth -Full" -ForegroundColor White
    Write-Host "   Get-Help Test-PostgreSQLPrimary -Examples" -ForegroundColor White
    
    # Add to profile (optional)
    Write-Host "`n💡 Tip: Add to PowerShell profile for auto-import:" -ForegroundColor Yellow
    Write-Host "   Add-Content `$PROFILE 'Import-Module $moduleName'" -ForegroundColor Gray
    
} catch {
    Write-Host "`n❌ Installation failed!" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.ScriptStackTrace)" -ForegroundColor Gray
    exit 1
}
